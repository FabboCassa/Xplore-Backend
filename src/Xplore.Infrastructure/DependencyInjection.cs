namespace Xplore.Infrastructure;

using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using Xplore.Application.AI;
using Xplore.Infrastructure.AI;
using Xplore.Infrastructure.Notifications;

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
        var useMockServices = string.Equals(configuration["AI:UseMock"], "true", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(openAiApiKey) && !useMockServices)
        {
            // Use real OpenAI services
            var kernelBuilder = Kernel.CreateBuilder();
            
            kernelBuilder.AddOpenAIChatCompletion(openAiModel, openAiApiKey);
            
            #pragma warning disable SKEXP0010 // Embedding services are experimental
            kernelBuilder.AddOpenAITextEmbeddingGeneration(embeddingModel, openAiApiKey);
            #pragma warning restore SKEXP0010

            var kernel = kernelBuilder.Build();
            services.AddSingleton(kernel);

            // Register real AI services
            services.AddSingleton<ITextGenerationService, SemanticKernelTextGenerationService>();
            
            #pragma warning disable SKEXP0001
            services.AddSingleton(sp => kernel.GetRequiredService<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>());
            #pragma warning restore SKEXP0001
            
            services.AddSingleton<IEmbeddingService, SemanticKernelEmbeddingService>();
        }
        else
        {
            // Use mock services for development without OpenAI
            services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
            services.AddSingleton<ITextGenerationService, MockTextGenerationService>();
        }
        
        // Register Vector Search Service (requires QdrantClient from Aspire)
        services.AddSingleton<IVectorSearchService>(sp =>
        {
            var qdrantClient = sp.GetService<QdrantClient>();
            var embeddingService = sp.GetRequiredService<IEmbeddingService>();
            var logger = sp.GetRequiredService<ILogger<QdrantVectorSearchService>>();
            
            if (qdrantClient != null)
            {
                return new QdrantVectorSearchService(qdrantClient, embeddingService, logger);
            }
            
            // Return null if Qdrant is not configured - ChatController handles this gracefully
            return null!;
        });

        // Register Overpass API Service (stateless POI proxy for maps)
        // HttpClient is scoped to OverpassApiService only — not registered globally
        services.AddMemoryCache();
        
        services.AddHttpClient<Xplore.Infrastructure.Map.WikipediaApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddSingleton<Xplore.Infrastructure.Map.OverpassApiService>(sp =>
        {
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var wikiService = sp.GetRequiredService<Xplore.Infrastructure.Map.WikipediaApiService>();
            var logger = sp.GetRequiredService<ILogger<Xplore.Infrastructure.Map.OverpassApiService>>();
            return new Xplore.Infrastructure.Map.OverpassApiService(httpClient, wikiService, logger);
        });

        // Register Email Sender
        services.AddTransient<Xplore.Application.Services.IEmailSender, Xplore.Infrastructure.Services.SmtpEmailSender>();

        // --- Firebase Cloud Messaging ---
        if (FirebaseApp.DefaultInstance == null)
        {
            GoogleCredential? credential = null;

            // Option 1: file path (preferred for local dev — avoids JSON escaping issues)
            var firebaseJsonPath = configuration["Firebase:ServiceAccountJsonPath"];
            if (!string.IsNullOrEmpty(firebaseJsonPath) && File.Exists(firebaseJsonPath))
            {
                credential = GoogleCredential.FromFile(firebaseJsonPath);
            }

            // Option 2: inline JSON (for production / environment variables)
            if (credential == null)
            {
                var firebaseJson = configuration["Firebase:ServiceAccountJson"];
                if (!string.IsNullOrEmpty(firebaseJson))
                    credential = GoogleCredential.FromJson(firebaseJson);
            }

            if (credential != null)
                FirebaseApp.Create(new AppOptions { Credential = credential });
        }

        services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();

        return services;
    }
}
