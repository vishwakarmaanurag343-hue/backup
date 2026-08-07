using System;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Dtos;
using Clausio.Legal.Core.Interfaces.AI;
using Clausio.Legal.Core.Interfaces.AI.Handlers;
using Clausio.Legal.Core.Interfaces.Memory;

namespace Clausio.Legal.Service.Ai.Handlers;

public class ChatHandler : IChatHandler
{
    private readonly IAIRouter _aiRouter;
    private readonly IContextEngine _contextEngine;
    private readonly IPromptBuilder _promptBuilder;

    public ChatHandler(IAIRouter aiRouter, IContextEngine contextEngine, IPromptBuilder promptBuilder)
    {
        _aiRouter = aiRouter;
        _contextEngine = contextEngine;
        _promptBuilder = promptBuilder;
    }

    public async Task<string> HandleAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildChatContextAsync(request.CaseId.GetValueOrDefault(), request.Message ?? "", cancellationToken);
        
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat");
        
        // Inject dynamic context into the system prompt 
        // (In a real setup we might pass variables, but here we append for simplicity)
        systemPrompt += $"\n\nContext:\n{contextXml}";

        var safePrompt = request.Message ?? string.Empty;
        return await _aiRouter.CompleteAsync(systemPrompt, safePrompt, "chat", cancellationToken);
    }
}
