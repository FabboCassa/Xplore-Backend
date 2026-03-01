namespace Xplore.Worker.Consumers;

using MassTransit;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using UglyToad.PdfPig;
using Xplore.Application.AI;
using Xplore.Contracts;

/// <summary>
/// Consumer that processes uploaded documents for RAG indexing.
/// </summary>
public class DocumentUploadedConsumer : IConsumer<DocumentUploadedEvent>
{
    private const string CollectionName = "museum_documents";
    
    private readonly ILogger<DocumentUploadedConsumer> _logger;
    private readonly IEmbeddingService? _embeddingService;
    private readonly QdrantClient? _qdrantClient;

    public DocumentUploadedConsumer(
        ILogger<DocumentUploadedConsumer> logger,
        IEmbeddingService? embeddingService = null,
        QdrantClient? qdrantClient = null)
    {
        _logger = logger;
        _embeddingService = embeddingService;
        _qdrantClient = qdrantClient;
    }

    public async Task Consume(ConsumeContext<DocumentUploadedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Document received for processing: {FileName} (ID: {DocumentId}, Museum: {MuseumId})",
            message.FileName, message.DocumentId, message.MuseumId);

        try
        {
            // Step 1: Read and extract text from PDF using PdfPig
            var text = ExtractTextFromPdf(message.FilePath);
            _logger.LogInformation("Extracted {CharCount} characters from {FileName}", 
                text.Length, message.FileName);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("No text extracted from PDF {FileName}", message.FileName);
                return;
            }

            // Step 2: Split into chunks
            var chunks = SplitIntoChunks(text, maxChunkSize: 1000);
            _logger.LogInformation("Split into {ChunkCount} chunks", chunks.Count);

            // Step 3: Generate embeddings and store in Qdrant
            if (_embeddingService != null && _qdrantClient != null)
            {
                // Ensure collection exists
                await EnsureCollectionExistsAsync(context.CancellationToken);
                
                var points = new List<PointStruct>();
                
                foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
                {
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk, context.CancellationToken);
                    
                    var pointId = Guid.NewGuid();
                    var point = new PointStruct
                    {
                        Id = new PointId { Uuid = pointId.ToString() },
                        Vectors = embedding.ToArray(),
                        Payload =
                        {
                            ["document_id"] = message.DocumentId.ToString(),
                            ["museum_id"] = message.MuseumId.ToString(),
                            ["file_name"] = message.FileName,
                            ["chunk_index"] = index,
                            ["text"] = chunk
                        }
                    };
                    points.Add(point);
                    
                    _logger.LogDebug("Generated embedding for chunk {Index}/{Total}", index + 1, chunks.Count);
                }
                
                // Batch upsert to Qdrant
                await _qdrantClient.UpsertAsync(CollectionName, points, cancellationToken: context.CancellationToken);
                
                _logger.LogInformation("Document {DocumentId} processed: {ChunkCount} chunks stored in Qdrant", 
                    message.DocumentId, points.Count);
            }
            else
            {
                if (_embeddingService == null)
                    _logger.LogWarning("EmbeddingService not configured. Configure OpenAI:ApiKey.");
                if (_qdrantClient == null)
                    _logger.LogWarning("QdrantClient not configured.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document {DocumentId}", message.DocumentId);
            throw; // Let MassTransit handle retry
        }
    }

    /// <summary>
    /// Ensures the Qdrant collection exists, creating it if necessary.
    /// </summary>
    private async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var collections = await _qdrantClient!.ListCollectionsAsync(cancellationToken);
            if (!collections.Contains(CollectionName))
            {
                // OpenAI text-embedding-3-small produces 1536-dimensional vectors
                await _qdrantClient.CreateCollectionAsync(
                    CollectionName,
                    new VectorParams { Size = 1536, Distance = Distance.Cosine },
                    cancellationToken: cancellationToken);
                
                _logger.LogInformation("Created Qdrant collection: {CollectionName}", CollectionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not verify/create Qdrant collection");
        }
    }

    /// <summary>
    /// Extracts text from a PDF file using PdfPig.
    /// </summary>
    private string ExtractTextFromPdf(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("PDF file not found: {FilePath}", filePath);
            return string.Empty;
        }

        try
        {
            using var document = PdfDocument.Open(filePath);
            var textBuilder = new System.Text.StringBuilder();
            
            foreach (var page in document.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }
            
            return textBuilder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF: {FilePath}", filePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Splits text into chunks for embedding with overlap.
    /// </summary>
    private static List<string> SplitIntoChunks(string text, int maxChunkSize)
    {
        var chunks = new List<string>();
        
        if (string.IsNullOrEmpty(text))
            return chunks;

        // Split by sentences for better semantic coherence
        var sentences = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var currentChunk = new List<string>();
        var currentLength = 0;

        foreach (var sentence in sentences)
        {
            var sentenceWithPeriod = sentence.Trim() + ".";
            
            if (currentLength + sentenceWithPeriod.Length > maxChunkSize && currentChunk.Count > 0)
            {
                chunks.Add(string.Join(" ", currentChunk));
                // Keep last sentence for overlap
                currentChunk = currentChunk.Count > 0 ? [currentChunk[^1]] : [];
                currentLength = currentChunk.Sum(s => s.Length + 1);
            }
            
            currentChunk.Add(sentenceWithPeriod);
            currentLength += sentenceWithPeriod.Length + 1;
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(string.Join(" ", currentChunk));
        }

        return chunks;
    }
}
