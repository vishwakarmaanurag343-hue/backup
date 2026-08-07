using System;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Dtos;
using Clausio.Legal.Core.Interfaces.AI;
using Clausio.Legal.Core.Interfaces.AI.Handlers;
using Clausio.Legal.Core.Interfaces.Memory;

namespace Clausio.Legal.Service.Ai.Handlers;

public class DraftingHandler : IDraftingHandler
{
    private readonly IAIRouter _aiRouter;
    private readonly IContextEngine _contextEngine;
    private readonly IPromptBuilder _promptBuilder;

    public DraftingHandler(IAIRouter aiRouter, IContextEngine contextEngine, IPromptBuilder promptBuilder)
    {
        _aiRouter = aiRouter;
        _contextEngine = contextEngine;
        _promptBuilder = promptBuilder;
    }

    public async Task<string> HandleDocumentAsync(Guid caseId, DraftRequestDto request, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildDraftingContextAsync(caseId, request.DraftType ?? "Document", request.Instructions ?? "", cancellationToken);
        
        var systemPrompt = _promptBuilder.BuildSystemPrompt("LegalDraft");
        systemPrompt += $"\n\nContext:\n{contextXml}\n\nDocument to Draft: {request.DraftType}";

        return await _aiRouter.CompleteAsync(systemPrompt, request.Instructions ?? "Draft the document based on the context.", "LegalDraft", cancellationToken);
    }

    public async Task<string> HandleWhatsAppAsync(Guid caseId, WhatsAppRequestDto request, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildDraftingContextAsync(caseId, "WhatsApp Update", request.Tone ?? "", cancellationToken);
        
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat"); // We could have a WhatsApp template later
        systemPrompt += $"\n\nContext:\n{contextXml}\n\nInstruction: Draft a professional WhatsApp update for the client.";

        return await _aiRouter.CompleteAsync(systemPrompt, request.Tone ?? "", "Drafting", cancellationToken);
    }
}
