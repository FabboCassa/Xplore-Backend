namespace Xplore.Worker.Consumers;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xplore.Contracts;
using Xplore.Domain.Entities;
using Xplore.Infrastructure.Persistence;

/// <summary>
/// Consumer that processes chat interactions for analytics and persistence.
/// </summary>
public class InteractionCreatedConsumer : IConsumer<InteractionCreatedEvent>
{
    private readonly ILogger<InteractionCreatedConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public InteractionCreatedConsumer(
        ILogger<InteractionCreatedConsumer> logger,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<InteractionCreatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Interaction received for analytics: {InteractionId} (Museum: {MuseumId})",
            message.InteractionId, message.MuseumId);

        try
        {
            // Create entity from event
            var interaction = new ChatInteraction
            {
                Id = message.InteractionId,
                MuseumId = message.MuseumId,
                SessionId = message.SessionId,
                UserQuestion = message.UserQuestion,
                AiResponse = message.AiResponse,
                RetrievedContext = message.RetrievedContext,
                IsAiGenerated = message.IsAiGenerated,
                ResponseTimeMs = message.ResponseTimeMs,
                CreatedAt = message.CreatedAt
            };

            // TODO: Add sentiment analysis here
            // interaction.Sentiment = await _sentimentService.AnalyzeAsync(message.UserQuestion);

            // TODO: Add topic tagging here
            // interaction.Tags = _topicTagger.Tag(message.UserQuestion);

            // Save to PostgreSQL
            _dbContext.ChatInteractions.Add(interaction);
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Interaction {InteractionId} saved to database", message.InteractionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save interaction {InteractionId}", message.InteractionId);
            throw; // Let MassTransit handle retry
        }
    }
}
