using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Dtos;
using Clausio.Legal.Core.Interfaces.AI.Pipeline;

namespace Clausio.Legal.Service;

public interface IAiService
{
    Task<string> SummarizeCaseAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> GenerateChronologyAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> DetectContradictionsAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> AnalyzeEvidenceAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<string> ResearchAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> GenerateActionPlanAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> TranslateAsync(TranslateRequest request, CancellationToken cancellationToken = default);
    Task<string> ChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
    Task<string> DraftWhatsAppAsync(Guid caseId, WhatsAppRequestDto request, CancellationToken cancellationToken = default);
    Task<string> AnalyzeFinancialsAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> AssessReadinessAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> EmergencyTriageAsync(EmergencyRequestDto request, CancellationToken cancellationToken = default);
    Task<string> PrepHearingAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> PrepWitnessAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> ClassifyCaseTypeAsync(CaseTypeRequestDto request, CancellationToken cancellationToken = default);
    Task<string> DraftDocumentAsync(Guid caseId, DraftRequestDto request, CancellationToken cancellationToken = default);
}

public class AiService : IAiService
{
    private readonly IAIPipeline _pipeline;

    public AiService(IAIPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<string> SummarizeCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(caseId, "Prepare a comprehensive case summary brief.", "Summarization", null, cancellationToken);

    public Task<string> GenerateChronologyAsync(Guid caseId, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(caseId, "Construct a comprehensive chronological timeline.", "Analysis", null, cancellationToken);

    public Task<string> DetectContradictionsAsync(Guid caseId, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(caseId, "Detect contradictions in the provided evidence and statements.", "Analysis", null, cancellationToken);

    public Task<string> AnalyzeEvidenceAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(documentId, "Analyze the specific evidence contained in this document.", "Analysis", null, cancellationToken);

    public Task<string> ResearchAsync(Guid caseId, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(caseId, "Conduct legal research for this case.", "Analysis", null, cancellationToken);

    public Task<string> GenerateActionPlanAsync(Guid caseId, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(caseId, "Generate a strategic action plan.", "ActionPlan", null, cancellationToken);

    public Task<string> TranslateAsync(TranslateRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object> { { "DocumentType", "Translation" } };
        return _pipeline.ExecuteAsync(Guid.Empty, $"Translate the following text to English: {request.Text}", "LegalDraft", parameters, cancellationToken);
    }

    public Task<string> ChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(request.CaseId.GetValueOrDefault(), request.Message ?? "", "chat", null, cancellationToken);
        
    public IAsyncEnumerable<string> StreamChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        => _pipeline.StreamExecuteAsync(request.CaseId.GetValueOrDefault(), request.Message ?? "", "chat", null, cancellationToken);

    public Task<string> DraftWhatsAppAsync(Guid caseId, WhatsAppRequestDto request, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object> { { "DocumentType", "WhatsApp Update" } };
        return _pipeline.ExecuteAsync(caseId, request.Tone ?? "Draft a professional WhatsApp update.", "LegalDraft", parameters, cancellationToken);
    }

    public Task<string> AnalyzeFinancialsAsync(Guid caseId, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(caseId, "Analyze the financial implications.", "Analysis", null, cancellationToken);

    public Task<string> AssessReadinessAsync(Guid caseId, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(caseId, "Assess case readiness for trial.", "Analysis", null, cancellationToken);

    public Task<string> EmergencyTriageAsync(EmergencyRequestDto request, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(Guid.Empty, $"Perform an emergency triage for the following critical update: {request.Query}", "ActionPlan", null, cancellationToken);

    public Task<string> PrepHearingAsync(Guid caseId, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(caseId, "Prepare a hearing brief.", "Analysis", null, cancellationToken);

    public Task<string> PrepWitnessAsync(Guid caseId, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(caseId, "Prepare a witness cross-examination guide.", "Analysis", null, cancellationToken);

    public Task<string> ClassifyCaseTypeAsync(CaseTypeRequestDto request, CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(Guid.Empty, $"Classify the legal nature, jurisdiction, and priority of this case based on the following context: {request.Description}", "Analysis", null, cancellationToken);

    public Task<string> DraftDocumentAsync(Guid caseId, DraftRequestDto request, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object> { { "DocumentType", request.DraftType ?? "Document" } };
        return _pipeline.ExecuteAsync(caseId, request.Instructions ?? "Draft the document.", "LegalDraft", parameters, cancellationToken);
    }
}
