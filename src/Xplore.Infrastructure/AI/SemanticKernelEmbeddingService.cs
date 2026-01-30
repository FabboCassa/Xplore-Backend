namespace Xplore.Infrastructure.AI;

using Microsoft.SemanticKernel.Embeddings;
using Xplore.Application.AI;

#pragma warning disable SKEXP0001 // Embedding services are experimental

/// <summary>
/// OpenAI-based embedding generation using Semantic Kernel.
/// </summary>
public class SemanticKernelEmbeddingService : IEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public SemanticKernelEmbeddingService(ITextEmbeddingGenerationService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync([text], cancellationToken: cancellationToken);
        return embeddings[0];
    }
}

#pragma warning restore SKEXP0001
