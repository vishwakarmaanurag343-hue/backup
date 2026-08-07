using System;

namespace Clausio.Legal.Core.Entities.AI;

public class AiTelemetryLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Request metadata
    public Guid CaseId { get; set; }
    public string Intent { get; set; } = string.Empty;
    public string PromptName { get; set; } = string.Empty;
    
    // Model selection
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string RouterDecision { get; set; } = string.Empty;
    
    // Performance
    public long LatencyMs { get; set; }
    public int TokensIn { get; set; }
    public int TokensOut { get; set; }
    public int TotalTokens => TokensIn + TokensOut;
    
    // Intelligence metrics
    public int RetrievalScore { get; set; }
    public int CitationConfidenceScore { get; set; }
    public int DraftScore { get; set; }
    public int HallucinationRiskScore { get; set; }
    public int TokenEfficiencyScore { get; set; }
    
    // Status
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
