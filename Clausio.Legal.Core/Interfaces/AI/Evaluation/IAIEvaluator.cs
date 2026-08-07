using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.AI.Evaluation;

public class AIEvaluationResult
{
    public int RetrievalQualityScore { get; set; }
    public int CitationConfidenceScore { get; set; }
    public int DraftQualityScore { get; set; }
    public int HallucinationRiskScore { get; set; }
    public int TokenEfficiencyScore { get; set; }
    public long LatencyMs { get; set; }
    public string Feedback { get; set; } = string.Empty;
}

public interface IAIEvaluator
{
    Task<AIEvaluationResult> EvaluateResponseAsync(
        string systemPrompt,
        string userPrompt,
        string aiResponse,
        long latencyMs,
        CancellationToken cancellationToken = default);
}
