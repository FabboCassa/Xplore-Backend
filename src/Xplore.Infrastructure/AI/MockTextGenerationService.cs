namespace Xplore.Infrastructure.AI;

using Microsoft.Extensions.Logging;
using Xplore.Application.AI;

/// <summary>
/// Mock text generation service for development/testing without OpenAI.
/// Returns context-aware fake responses.
/// </summary>
public class MockTextGenerationService : ITextGenerationService
{
    private readonly ILogger<MockTextGenerationService> _logger;

    public MockTextGenerationService(ILogger<MockTextGenerationService> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateResponseAsync(string query, string context, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[MOCK] Generating fake response for: {Query}", query);

        // Generate a mock response that includes parts of the context
        var contextPreview = context.Length > 200 ? context[..200] + "..." : context;
        
        var response = $"""
            🧪 [RISPOSTA MOCK - Modalità Sviluppo]
            
            La tua domanda era: "{query}"
            
            In un sistema reale, l'AI analizzerebbe il seguente contesto recuperato da Qdrant:
            ---
            {contextPreview}
            ---
            
            E genererebbe una risposta intelligente basata su queste informazioni.
            
            Per abilitare le risposte AI reali, configura una API Key OpenAI valida con credito disponibile.
            """;

        return Task.FromResult(response);
    }
}
