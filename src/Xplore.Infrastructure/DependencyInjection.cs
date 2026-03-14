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

            // Option 1: file path (local dev via User Secrets or appsettings)
            var firebaseJsonPath = configuration["Firebase:ServiceAccountJsonPath"];
            Console.WriteLine($"[Firebase] ServiceAccountJsonPath from config: '{firebaseJsonPath}'");

            if (!string.IsNullOrEmpty(firebaseJsonPath))
            {
                // Try the path as-is first (absolute paths from User Secrets)
                if (File.Exists(firebaseJsonPath))
                {
                    Console.WriteLine($"[Firebase] Loading credentials from: {firebaseJsonPath}");
                    credential = GoogleCredential.FromFile(firebaseJsonPath);
                }
                else
                {
                    // Try relative to AppContext.BaseDirectory
                    var fullPath = Path.Combine(AppContext.BaseDirectory, firebaseJsonPath);
                    Console.WriteLine($"[Firebase] File not found at '{firebaseJsonPath}', trying: {fullPath}");
                    if (File.Exists(fullPath))
                    {
                        Console.WriteLine($"[Firebase] Loading credentials from: {fullPath}");
                        credential = GoogleCredential.FromFile(fullPath);
                    }
                    else
                    {
                        Console.WriteLine($"[Firebase] File not found at either location.");
                    }
                }
            }

            // Option 2: inline JSON (production / environment variables)
            if (credential == null)
            {
                var firebaseJson = configuration["Firebase:ServiceAccountJson"];
                if (!string.IsNullOrEmpty(firebaseJson))
                {
                    Console.WriteLine("[Firebase] Loading credentials from inline JSON.");
                    credential = GoogleCredential.FromJson(firebaseJson);
                }
            }

            if (credential != null)
            {
                FirebaseApp.Create(new AppOptions { Credential = credential });
                Console.WriteLine("[Firebase] SDK initialized successfully.");
            }
            else
            {
                Console.WriteLine("[Firebase] SDK NOT initialized — no credentials found. Push notifications will NOT work.");
            }
        }

        services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();

        return services;
    }
}
