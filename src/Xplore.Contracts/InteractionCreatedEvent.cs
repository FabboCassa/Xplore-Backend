namespace Xplore.Contracts;

/// <summary>
/// Event published when a chat interaction occurs.
/// Consumed by Worker for analytics processing and persistence.
/// </summary>
public record InteractionCreatedEvent(
    Guid InteractionId,
    Guid MuseumId,
    Guid? SessionId,
    string UserQuestion,
    string AiResponse,
    string? RetrievedContext,
    bool IsAiGenerated,
    int ResponseTimeMs,
    DateTime CreatedAt
);
