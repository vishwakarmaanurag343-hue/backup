using System.Collections.Generic;
using System.Linq;
using Clausio.MCP.Interfaces;
using Clausio.MCP.Registry;

namespace Clausio.MCP.Planners;

public class CapabilityPlanner
{
    private readonly IEnumerable<IMcpSkill> _allSkills;

    public CapabilityPlanner(IEnumerable<IMcpSkill> allSkills)
    {
        _allSkills = allSkills;
    }

    public List<IMcpSkill> SelectSkillsForWorkflow(WorkflowType workflow, ModelCapability modelCapability)
    {
        // If model does not support tool calling, return empty list to prevent sending tools to LLM
        if (!modelCapability.ToolCalling)
        {
            return new List<IMcpSkill>();
        }

        return workflow switch
        {
            WorkflowType.SimpleChat => _allSkills.Where(s => s.Name == "MemorySkill" || s.Name == "ResearchSkill").ToList(),
            WorkflowType.LegalDraft => _allSkills.Where(s => s.Name == "DraftSkill" || s.Name == "CitationSkill" || s.Name == "ResearchSkill").ToList(),
            WorkflowType.ContractReview => _allSkills.Where(s => s.Name == "DraftSkill" || s.Name == "ResearchSkill").ToList(),
            WorkflowType.DeepResearch => _allSkills.Where(s => s.Name == "ResearchSkill" || s.Name == "CitationSkill").ToList(),
            WorkflowType.OcrAnalysis => _allSkills.Where(s => s.Name == "OcrSkill").ToList(),
            WorkflowType.TimelineAnalysis => _allSkills.Where(s => s.Name == "TimelineSkill" || s.Name == "ResearchSkill").ToList(),
            WorkflowType.RiskAssessment => _allSkills.Where(s => s.Name == "ResearchSkill" || s.Name == "MemorySkill").ToList(),
            _ => _allSkills.Where(s => s.Name == "ResearchSkill").ToList()
        };
    }
}
