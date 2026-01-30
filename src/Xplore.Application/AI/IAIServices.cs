namespace Xplore.Application.AI;

/// <summary>
/// Abstraction for AI text generation services.
/// </summary>
public interface ITextGenerationService
{
    /// <summary>
    /// Generates a response using RAG (Retrieval-Augmented Generation).
    /// </summary>
    /// <param name="query">The user's question.</param>
    /// <param name="context">Retrieved context from vector database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated AI response.</returns>
    Task<string> GenerateResponseAsync(string query, string context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction for embedding generation services.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates embeddings for the given text.
    /// </summary>
    /// <param name="text">Text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Vector embeddings.</returns>
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction for vector search operations.
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// Searches for similar documents based on query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="limit">Maximum results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of relevant text chunks.</returns>
    Task<IReadOnlyList<string>> SearchAsync(string query, int limit = 5, CancellationToken cancellationToken = default);
}
