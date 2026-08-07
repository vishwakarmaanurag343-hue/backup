using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.AI;
using Clausio.Legal.Core.Interfaces.AI.Drafting;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.AI.Drafting;

public class DraftReviewResult
{
    public bool QualityPassed { get; set; }
    public int OverallScore { get; set; }
    public string Recommendation { get; set; } = "Accept";
}

public class DraftEngine : IDraftEngine
{
    private readonly IPromptBuilder _promptBuilder;
    private readonly IAIRouter _aiRouter;
    private readonly IDraftValidationPipeline _validationPipeline;
    private readonly ILogger<DraftEngine> _logger;

    public DraftEngine(
        IPromptBuilder promptBuilder, 
        IAIRouter aiRouter, 
        IDraftValidationPipeline validationPipeline,
        ILogger<DraftEngine> logger)
    {
        _promptBuilder = promptBuilder;
        _aiRouter = aiRouter;
        _validationPipeline = validationPipeline;
        _logger = logger;
    }

    public async Task<string> DraftDocumentAsync(Guid caseId, string documentType, string instructions, string contextXml, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DraftEngine] Starting draft. Type: {DocumentType}, Case: {CaseId}", documentType, caseId);

        // Step 1: Select specialized template
        var templateName = GetTemplateForDocumentType(documentType);
        _logger.LogInformation("[DraftEngine] Selected template: {Template}", templateName);

        var variables = new Dictionary<string, string>
        {
            { "CONTEXT", contextXml },
            { "INSTRUCTIONS", instructions }
        };
        var systemPrompt = _promptBuilder.BuildSystemPrompt(templateName, variables);

        // Step 2: Generate initial draft
        _logger.LogInformation("[DraftEngine] Generating initial draft...");
        var initialDraft = await _aiRouter.CompleteAsync(systemPrompt, instructions, "LegalDraft", cancellationToken);

        _logger.LogInformation("[DraftEngine] Draft generated successfully.");
        return initialDraft;
    }



    private string GetTemplateForDocumentType(string documentType)
    {
        var lower = documentType.ToLowerInvariant();
        return lower switch
        {
            var t when t.Contains("notice") => "Drafts/Notice",
            var t when t.Contains("consumer") => "Drafts/ConsumerComplaint",
            var t when t.Contains("complaint") => "Drafts/ConsumerComplaint",
            var t when t.Contains("agreement") => "Drafts/Agreement",
            var t when t.Contains("affidavit") => "Drafts/Affidavit",
            var t when t.Contains("petition") => "Drafts/Petition",
            var t when t.Contains("nda") => "Drafts/Agreement",
            var t when t.Contains("employment") => "Drafts/Agreement",
            var t when t.Contains("lease") => "Drafts/Agreement",
            var t when t.Contains("opinion") => "Drafts/LegalOpinion",
            var t when t.Contains("written statement") => "Drafts/WrittenStatement",
            var t when t.Contains("reply") => "Drafts/WrittenStatement",
            var t when t.Contains("risk") => "Analysis/RiskAssessment",
            var t when t.Contains("clause") => "Analysis/ClauseAnalysis",
            _ => "LegalDraft"
        };
    }
}
