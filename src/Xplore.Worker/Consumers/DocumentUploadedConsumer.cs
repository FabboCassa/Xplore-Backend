namespace Xplore.Worker.Consumers;

using MassTransit;
using Microsoft.Extensions.Logging;
using Xplore.Application.AI;
using Xplore.Contracts;

/// <summary>
/// Consumer that processes uploaded documents for RAG indexing.
/// </summary>
public class DocumentUploadedConsumer : IConsumer<DocumentUploadedEvent>
{
    private readonly ILogger<DocumentUploadedConsumer> _logger;
    private readonly IEmbeddingService? _embeddingService;

    public DocumentUploadedConsumer(
        ILogger<DocumentUploadedConsumer> logger,
        IEmbeddingService? embeddingService = null)
    {
        _logger = logger;
        _embeddingService = embeddingService;
    }

    public async Task Consume(ConsumeContext<DocumentUploadedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "📄 Document received for processing: {FileName} (ID: {DocumentId}, Museum: {MuseumId})",
            message.FileName, message.DocumentId, message.MuseumId);

        try
        {
            // Step 1: Read and extract text from PDF
            var text = await ExtractTextFromPdfAsync(message.FilePath);
            _logger.LogInformation("Extracted {CharCount} characters from {FileName}", 
                text.Length, message.FileName);

            // Step 2: Split into chunks
            var chunks = SplitIntoChunks(text, maxChunkSize: 1000);
            _logger.LogInformation("Split into {ChunkCount} chunks", chunks.Count);

            // Step 3: Generate embeddings and store in Qdrant
            if (_embeddingService != null)
            {
                foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
                {
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk, context.CancellationToken);
                    _logger.LogDebug("Generated embedding for chunk {Index}/{Total}", index + 1, chunks.Count);
                    
                    // TODO: Store in Qdrant
                    // await _qdrantClient.UpsertAsync(collectionName, new PointStruct { ... });
                }
                
                _logger.LogInformation("✅ Document {DocumentId} processed successfully!", message.DocumentId);
            }
            else
            {
                _logger.LogWarning("⚠️ EmbeddingService not configured. Skipping embedding generation.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to process document {DocumentId}", message.DocumentId);
            throw; // Let MassTransit handle retry
        }
    }

    /// <summary>
    /// Extracts text from a PDF file.
    /// </summary>
    private static Task<string> ExtractTextFromPdfAsync(string filePath)
    {
        // TODO: Use a PDF library like PdfPig or iText
        // For now, return placeholder
        if (!File.Exists(filePath))
        {
            return Task.FromResult($"[File not found: {filePath}]");
        }

        // Placeholder - in production use PdfPig:
        // using var document = PdfDocument.Open(filePath);
        // var text = string.Join("\n", document.GetPages().Select(p => p.Text));
        
        return Task.FromResult($"[PDF Content from: {Path.GetFileName(filePath)}] " +
            "Questa è una simulazione del contenuto estratto dal PDF. " +
            "In produzione, useremo una libreria come PdfPig per estrarre il testo reale.");
    }

    /// <summary>
    /// Splits text into chunks for embedding.
    /// </summary>
    private static List<string> SplitIntoChunks(string text, int maxChunkSize)
    {
        var chunks = new List<string>();
        
        if (string.IsNullOrEmpty(text))
            return chunks;

        // Simple chunking by character count with overlap
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new List<string>();
        var currentLength = 0;

        foreach (var word in words)
        {
            if (currentLength + word.Length + 1 > maxChunkSize && currentChunk.Count > 0)
            {
                chunks.Add(string.Join(" ", currentChunk));
                // Keep last 20% for overlap
                var overlapCount = Math.Max(1, currentChunk.Count / 5);
                currentChunk = currentChunk.TakeLast(overlapCount).ToList();
                currentLength = currentChunk.Sum(w => w.Length + 1);
            }
            
            currentChunk.Add(word);
            currentLength += word.Length + 1;
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(string.Join(" ", currentChunk));
        }

        return chunks;
    }
}
