namespace Xplore.Infrastructure.AI;

using Microsoft.Extensions.Logging;
using Xplore.Application.AI;

/// <summary>
/// Mock embedding service for development/testing without OpenAI.
/// Generates deterministic fake embeddings based on text hash.
/// </summary>
public class MockEmbeddingService : IEmbeddingService
{
    private const int EmbeddingDimensions = 1536; // Same as text-embedding-3-small
    private readonly ILogger<MockEmbeddingService> _logger;

    public MockEmbeddingService(ILogger<MockEmbeddingService> logger)
    {
        _logger = logger;
    }

    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[MOCK] Generating fake embedding for: {Preview}...", 
            text.Length > 50 ? text[..50] : text);

        // Generate deterministic embeddings based on text hash
        // This ensures the same text always produces the same vector (important for search)
        var hash = text.GetHashCode();
        var random = new Random(hash);
        
        var embedding = new float[EmbeddingDimensions];
        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Values between -1 and 1
        }

        // Normalize the vector (important for cosine similarity)
        var magnitude = MathF.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            embedding[i] /= magnitude;
        }

        return Task.FromResult<ReadOnlyMemory<float>>(embedding);
    }
}
