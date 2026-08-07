using System.Collections.Generic;

namespace Clausio.Legal.Core.Interfaces.AI.Pipeline;

public class AIPipelineContext
{
    public string Intent { get; set; } = string.Empty;
    public string Complexity { get; set; } = "Low";
    
    public string SystemPrompt { get; set; } = string.Empty;
    public string FinalUserPrompt { get; set; } = string.Empty;
    
    public string CaseMemoryXml { get; set; } = string.Empty;
    public string RetrievedEvidenceXml { get; set; } = string.Empty;
    
    public string ModelUsed { get; set; } = string.Empty;
    public int TokenCountEstimate { get; set; }
    
    public Dictionary<string, object> Variables { get; set; } = new();
}
