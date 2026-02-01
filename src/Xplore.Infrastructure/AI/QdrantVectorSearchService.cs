namespace Xplore.Infrastructure.AI;

using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Xplore.Application.AI;

/// <summary>
/// Qdrant-based vector search service for RAG retrieval.
/// </summary>
public class QdrantVectorSearchService : IVectorSearchService
{
    private const string CollectionName = "museum_documents";
    
    private readonly QdrantClient _qdrantClient;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<QdrantVectorSearchService> _logger;

    public QdrantVectorSearchService(
        QdrantClient qdrantClient,
        IEmbeddingService embeddingService,
        ILogger<QdrantVectorSearchService> logger)
    {
        _qdrantClient = qdrantClient;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> SearchAsync(string query, int limit = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate embedding for the query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
            
            _logger.LogDebug("Searching Qdrant for: {Query}", query);

            // Search for similar vectors in Qdrant
            var searchResults = await _qdrantClient.SearchAsync(
                CollectionName,
                queryEmbedding.ToArray(),
                limit: (ulong)limit,
                cancellationToken: cancellationToken);

            // Extract text from the payload of each result
            var chunks = new List<string>();
            foreach (var result in searchResults)
            {
                if (result.Payload.TryGetValue("text", out var textValue))
                {
                    chunks.Add(textValue.StringValue);
                    _logger.LogDebug("Found chunk with score {Score}: {Preview}...", 
                        result.Score, 
                        textValue.StringValue.Length > 50 ? textValue.StringValue[..50] : textValue.StringValue);
                }
            }

            _logger.LogInformation("Found {Count} relevant chunks for query", chunks.Count);
            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search Qdrant for query: {Query}", query);
            return [];
        }
    }
}
