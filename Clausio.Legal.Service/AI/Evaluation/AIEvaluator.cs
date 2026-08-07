using System;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.AI;
using Clausio.Legal.Core.Interfaces.AI.Evaluation;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.AI.Evaluation;

public class AIEvaluator : IAIEvaluator
{
    private readonly ILogger<AIEvaluator> _logger;

    public AIEvaluator(ILogger<AIEvaluator> logger)
    {
        _logger = logger;
    }

    public Task<AIEvaluationResult> EvaluateResponseAsync(
        string systemPrompt,
        string userPrompt,
        string aiResponse,
        long latencyMs,
        CancellationToken cancellationToken = default)
    {
        // In a real system, this could asynchronously invoke a fast, cheap LLM 
        // to grade the AI response against the context.
        // For Phase 5, we will do a deterministic lightweight evaluation and log it.
        
        var result = new AIEvaluationResult
        {
            LatencyMs = latencyMs,
            TokenEfficiencyScore = CalculateTokenEfficiency(systemPrompt, userPrompt, aiResponse),
            CitationConfidenceScore = aiResponse.Contains("[Unverified:") ? 4 : 9,
            DraftQualityScore = 8, // Placeholder
            RetrievalQualityScore = 8, // Placeholder
            HallucinationRiskScore = 2, // 1-10 where lower is better
            Feedback = "Evaluation completed via heuristics."
        };

        _logger.LogInformation(
            "[AIEvaluator] Evaluation complete. Latency={LatencyMs}ms, Tokens={Tokens}, Citations={Citations}",
            result.LatencyMs, result.TokenEfficiencyScore, result.CitationConfidenceScore);

        return Task.FromResult(result);
    }

    private int CalculateTokenEfficiency(string system, string user, string response)
    {
        var totalLength = system.Length + user.Length + response.Length;
        return totalLength < 5000 ? 9 : (totalLength < 15000 ? 7 : 4);
    }
}
