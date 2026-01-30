namespace Xplore.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Xplore.Application.AI;
using Xplore.Infrastructure.AI;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services including Semantic Kernel and AI services.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Semantic Kernel Configuration ---
        var openAiApiKey = configuration["OpenAI:ApiKey"] ?? "";
        var openAiModel = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var embeddingModel = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

        if (!string.IsNullOrEmpty(openAiApiKey))
        {
            // Build Kernel with OpenAI services
            var kernelBuilder = Kernel.CreateBuilder();
            
            kernelBuilder.AddOpenAIChatCompletion(openAiModel, openAiApiKey);
            
            #pragma warning disable SKEXP0010 // Embedding services are experimental
            kernelBuilder.AddOpenAITextEmbeddingGeneration(embeddingModel, openAiApiKey);
            #pragma warning restore SKEXP0010

            var kernel = kernelBuilder.Build();
            services.AddSingleton(kernel);

            // Register AI services
            services.AddSingleton<ITextGenerationService, SemanticKernelTextGenerationService>();
            
            #pragma warning disable SKEXP0001
            services.AddSingleton(sp => kernel.GetRequiredService<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>());
            #pragma warning restore SKEXP0001
            
            services.AddSingleton<IEmbeddingService, SemanticKernelEmbeddingService>();
        }

        return services;
    }
}
