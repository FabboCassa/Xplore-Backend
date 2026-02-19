namespace Xplore.API.Controllers;

using System.Diagnostics;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xplore.Application.AI;
using Xplore.Contracts;

/// <summary>
/// Controller for AI-powered chat interactions using RAG.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ITextGenerationService? _textGenerationService;
    private readonly IVectorSearchService? _vectorSearchService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ILogger<ChatController> logger,
        IPublishEndpoint publishEndpoint,
        ITextGenerationService? textGenerationService = null,
        IVectorSearchService? vectorSearchService = null)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _textGenerationService = textGenerationService;
        _vectorSearchService = vectorSearchService;
    }

    /// <summary>
    /// Send a question to the AI assistant using RAG.
    /// </summary>
    /// <param name="request">The chat request with the user's question.</param>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question cannot be empty.");
        }

        _logger.LogInformation("💬 Received question: {Question}", request.Question);

        // Check if AI services are configured
        if (_textGenerationService == null)
        {
            _logger.LogWarning("AI services not configured. Returning fallback response.");
            return StatusCode(503, new ChatResponse(
                "Mi dispiace, il servizio AI non è configurato. Configura la API Key di OpenAI.",
                false,
                null));
        }

        try
        {
            // Step 1: Search for relevant context (RAG Retrieval)
            string context;
            if (_vectorSearchService != null)
            {
                var relevantChunks = await _vectorSearchService.SearchAsync(
                    request.Question, 
                    limit: 5);
                context = string.Join("\n\n", relevantChunks);
                _logger.LogInformation("Found {ChunkCount} relevant chunks", relevantChunks.Count);
            }
            else
            {
                // Fallback context for demo
                context = """
                    Il Museo Egizio di Torino è il più antico museo al mondo dedicato alla civiltà egizia.
                    Fondato nel 1824, ospita oltre 40.000 reperti che coprono un arco temporale di 4.000 anni.
                    Tra i pezzi più celebri vi è la tomba di Kha e Merit, perfettamente conservata.
                    Il museo offre visite guidate e laboratori interattivi per famiglie e scuole.
                    """;
                _logger.LogInformation("Using demo context (VectorSearchService not configured)");
            }

            // Step 2: Generate response using LLM (RAG Augmented Generation)
            var response = await _textGenerationService.GenerateResponseAsync(
                request.Question,
                context);

            stopwatch.Stop();
            _logger.LogInformation("✅ Generated AI response ({Length} chars) in {Ms}ms", 
                response.Length, stopwatch.ElapsedMilliseconds);

            // Step 3: Publish event for analytics (fire-and-forget, non-blocking)
            var interactionEvent = new InteractionCreatedEvent(
                InteractionId: Guid.NewGuid(),
                MuseumId: request.MuseumId ?? Guid.Empty,
                SessionId: request.SessionId,
                UserQuestion: request.Question,
                AiResponse: response,
                RetrievedContext: context.Length > 2000 ? context[..2000] : context, // Truncate for storage
                IsAiGenerated: true,
                ResponseTimeMs: (int)stopwatch.ElapsedMilliseconds,
                CreatedAt: DateTime.UtcNow
            );
            
            // Fire-and-forget: don't await, don't block the response
            _ = _publishEndpoint.Publish(interactionEvent);
            _logger.LogDebug("Published InteractionCreatedEvent for analytics");

            return Ok(new ChatResponse(response, true, context));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI response");
            return StatusCode(500, new ChatResponse(
                "Mi dispiace, si è verificato un errore nella generazione della risposta.",
                false,
                null));
        }
    }

    /// <summary>
    /// Health check for AI services.
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            TextGenerationConfigured = _textGenerationService != null,
            VectorSearchConfigured = _vectorSearchService != null,
            Message = _textGenerationService != null 
                ? "AI services are ready" 
                : "Configure OpenAI:ApiKey in user secrets or appsettings.json"
        });
    }
}

/// <summary>
/// Request body for chat endpoint.
/// </summary>
public record ChatRequest(string Question, Guid? MuseumId = null, Guid? SessionId = null);

/// <summary>
/// Response from chat endpoint.
/// </summary>
public record ChatResponse(string Answer, bool IsAiGenerated, string? Context);
