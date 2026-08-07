using System;

namespace Clausio.MCP.Planners;

public class WorkflowPlanner
{
    public WorkflowType PlanWorkflow(string userQuery)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            return WorkflowType.SimpleChat;

        var lower = userQuery.ToLowerInvariant();

        if (lower.Contains("draft") || lower.Contains("write an agreement") || lower.Contains("prepare petition"))
            return WorkflowType.LegalDraft;

        if (lower.Contains("review") || lower.Contains("clause") || lower.Contains("compare contract"))
            return WorkflowType.ContractReview;

        if (lower.Contains("precedent") || lower.Contains("case law") || lower.Contains("judgment") || lower.Contains("research"))
            return WorkflowType.DeepResearch;

        if (lower.Contains("ocr") || lower.Contains("scan") || lower.Contains("extract text"))
            return WorkflowType.OcrAnalysis;

        if (lower.Contains("timeline") || lower.Contains("dates") || lower.Contains("chronology"))
            return WorkflowType.TimelineAnalysis;

        if (lower.Contains("risk") || lower.Contains("chance of win") || lower.Contains("assessment"))
            return WorkflowType.RiskAssessment;

        return WorkflowType.SimpleChat;
    }
}
