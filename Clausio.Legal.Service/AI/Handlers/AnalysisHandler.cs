using System;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.AI;
using Clausio.Legal.Core.Interfaces.AI.Handlers;
using Clausio.Legal.Core.Interfaces.Memory;

namespace Clausio.Legal.Service.Ai.Handlers;

public class AnalysisHandler : IAnalysisHandler
{
    private readonly IAIRouter _aiRouter;
    private readonly IContextEngine _contextEngine;
    private readonly IPromptBuilder _promptBuilder;

    public AnalysisHandler(IAIRouter aiRouter, IContextEngine contextEngine, IPromptBuilder promptBuilder)
    {
        _aiRouter = aiRouter;
        _contextEngine = contextEngine;
        _promptBuilder = promptBuilder;
    }

    public async Task<string> SummarizeCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildAnalysisContextAsync(caseId, "Case Summary", cancellationToken);
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat") + $"\n\nContext:\n{contextXml}";
        return await _aiRouter.CompleteAsync(systemPrompt, "Prepare a comprehensive case summary brief based on the provided context. Format as a JSON string.", "Summarization", cancellationToken);
    }

    public async Task<string> GenerateChronologyAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildAnalysisContextAsync(caseId, "Chronology", cancellationToken);
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat") + $"\n\nContext:\n{contextXml}";
        return await _aiRouter.CompleteAsync(systemPrompt, "Construct a comprehensive chronological timeline based on the context. Return JSON.", "Analysis", cancellationToken);
    }

    public async Task<string> DetectContradictionsAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildAnalysisContextAsync(caseId, "Contradictions", cancellationToken);
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat") + $"\n\nContext:\n{contextXml}";
        return await _aiRouter.CompleteAsync(systemPrompt, "Detect contradictions in the provided evidence and statements. Return JSON.", "Analysis", cancellationToken);
    }

    public async Task<string> AnalyzeEvidenceAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult("{\"status\": \"Pending RAG document integration\"}");
    }

    public async Task<string> ResearchAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildAnalysisContextAsync(caseId, "Legal Research", cancellationToken);
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat") + $"\n\nContext:\n{contextXml}";
        return await _aiRouter.CompleteAsync(systemPrompt, "Conduct legal research for this case based on the provided context. Return JSON.", "Analysis", cancellationToken);
    }

    public async Task<string> GenerateActionPlanAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildAnalysisContextAsync(caseId, "Action Plan", cancellationToken);
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat") + $"\n\nContext:\n{contextXml}";
        return await _aiRouter.CompleteAsync(systemPrompt, "Generate a strategic action plan based on the case context. Return JSON.", "ActionPlan", cancellationToken);
    }

    public async Task<string> AnalyzeFinancialsAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildAnalysisContextAsync(caseId, "Financials", cancellationToken);
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat") + $"\n\nContext:\n{contextXml}";
        return await _aiRouter.CompleteAsync(systemPrompt, "Analyze the financial implications and details from the context. Return JSON.", "Analysis", cancellationToken);
    }

    public async Task<string> AssessReadinessAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildAnalysisContextAsync(caseId, "Readiness", cancellationToken);
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat") + $"\n\nContext:\n{contextXml}";
        return await _aiRouter.CompleteAsync(systemPrompt, "Assess case readiness for trial based on context. Return JSON.", "Analysis", cancellationToken);
    }

    public async Task<string> PrepHearingAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildAnalysisContextAsync(caseId, "Hearing Prep", cancellationToken);
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat") + $"\n\nContext:\n{contextXml}";
        return await _aiRouter.CompleteAsync(systemPrompt, "Prepare a hearing brief based on the context. Return JSON.", "Analysis", cancellationToken);
    }

    public async Task<string> PrepWitnessAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var contextXml = await _contextEngine.BuildAnalysisContextAsync(caseId, "Witness Prep", cancellationToken);
        var systemPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat") + $"\n\nContext:\n{contextXml}";
        return await _aiRouter.CompleteAsync(systemPrompt, "Prepare a witness cross-examination guide based on the context. Return JSON.", "Analysis", cancellationToken);
    }
}
