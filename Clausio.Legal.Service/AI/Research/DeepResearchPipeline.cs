using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Clausio.Legal.Core.Interfaces.AI;
using Clausio.Legal.Core.Interfaces.AI.Research;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.AI.Research;

public class DeepResearchPipeline : IDeepResearchPipeline
{
    private readonly IAIRouter _aiRouter;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IRetrievalEngine _retrievalEngine;
    private readonly ILogger<DeepResearchPipeline> _logger;

    public DeepResearchPipeline(
        IAIRouter aiRouter,
        IPromptBuilder promptBuilder,
        IRetrievalEngine retrievalEngine,
        ILogger<DeepResearchPipeline> logger)
    {
        _aiRouter = aiRouter;
        _promptBuilder = promptBuilder;
        _retrievalEngine = retrievalEngine;
        _logger = logger;
    }

    public async Task<string> ExecuteResearchAsync(Guid caseId, string researchQuery, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DeepResearch] Starting Deep Research for CaseId: {CaseId}. Query: {Query}", caseId, researchQuery);

        // 1. Plan
        _logger.LogInformation("[DeepResearch] Step 1: Research Plan");
        var planPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat"); // Would be a specific planner prompt in production
        var planResponse = await _aiRouter.CompleteAsync(planPrompt, $"Create a search plan for: {researchQuery}", "chat", cancellationToken);

        // 2. Retrieve
        _logger.LogInformation("[DeepResearch] Step 2: Retrieve");
        var chunks = await _retrievalEngine.GetContextAsync(researchQuery + " " + planResponse, caseId, cancellationToken);
        var evidence = string.Join("\n\n", chunks.Select(c => $"[Source: {c.DocumentType}] {c.TextContent}"));

        // 3. Reason & Write
        _logger.LogInformation("[DeepResearch] Step 3: Reason & Write");
        var variables = new System.Collections.Generic.Dictionary<string, string> { { "CONTEXT", evidence } };
        var writePrompt = _promptBuilder.BuildSystemPrompt("Analysis/LegalReasoning", variables); // Reuse reasoning for now
        var researchDraft = await _aiRouter.CompleteAsync(writePrompt, $"Execute this research query: {researchQuery}", "Analysis", cancellationToken);

        // 4. Review
        _logger.LogInformation("[DeepResearch] Step 4: Verify & Review");
        var reviewPrompt = _promptBuilder.BuildSystemPrompt("GeneralChat");
        var finalResult = await _aiRouter.CompleteAsync(reviewPrompt, $"Review and finalize this research report:\n\n{researchDraft}", "chat", cancellationToken);

        _logger.LogInformation("[DeepResearch] Research Complete.");
        return finalResult;
    }
}
