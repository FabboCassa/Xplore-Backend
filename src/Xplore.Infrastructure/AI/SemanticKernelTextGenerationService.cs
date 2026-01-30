namespace Xplore.Infrastructure.AI;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Xplore.Application.AI;

/// <summary>
/// OpenAI-based text generation using Semantic Kernel.
/// </summary>
public class SemanticKernelTextGenerationService : ITextGenerationService
{
    private readonly Kernel _kernel;

    public SemanticKernelTextGenerationService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> GenerateResponseAsync(string query, string context, CancellationToken cancellationToken = default)
    {
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("""
            Sei un assistente museale esperto e cordiale. Rispondi alle domande degli utenti 
            utilizzando SOLO le informazioni fornite nel contesto. Se non trovi informazioni 
            rilevanti nel contesto, dillo gentilmente. Rispondi in italiano.
            """);
        
        chatHistory.AddUserMessage($"""
            Contesto:
            {context}
            
            Domanda dell'utente:
            {query}
            """);

        var response = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
        
        return response.Content ?? "Mi dispiace, non sono riuscito a generare una risposta.";
    }
}
