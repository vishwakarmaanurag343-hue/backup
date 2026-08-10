using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.AI;
using Clausio.Legal.Core.Interfaces.AI.Drafting;
using Clausio.Legal.Core.Interfaces.AI.Evaluation;
using Clausio.Legal.Core.Interfaces.AI.Pipeline;
using Clausio.Legal.Core.Interfaces.AI.Validation;
using Clausio.Legal.Core.Interfaces.AI.Security;
using Clausio.Legal.Core.Interfaces.Memory;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.AI.Pipeline;

public class AIPipeline : IAIPipeline
{
    private readonly IContextEngine _contextEngine;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IAIRouter _router;
    private readonly IDraftEngine _draftEngine;
    private readonly ICitationVerifier _citationVerifier;
    private readonly IAIEvaluator _evaluator;
    private readonly IAISecurityLayer _securityLayer;
    private readonly ITelemetryService _telemetryService;
    private readonly Clausio.MCP.Planners.WorkflowPlanner _workflowPlanner;
    private readonly Clausio.MCP.Planners.CapabilityPlanner _capabilityPlanner;
    private readonly Clausio.MCP.Registry.AiCapabilityRegistry _capabilityRegistry;
    private readonly ILogger<AIPipeline> _logger;

    public AIPipeline(
        IContextEngine contextEngine,
        IPromptBuilder promptBuilder,
        IAIRouter router,
        IDraftEngine draftEngine,
        ICitationVerifier citationVerifier,
        IAIEvaluator evaluator,
        IAISecurityLayer securityLayer,
        ITelemetryService telemetryService,
        Clausio.MCP.Planners.WorkflowPlanner workflowPlanner,
        Clausio.MCP.Planners.CapabilityPlanner capabilityPlanner,
        Clausio.MCP.Registry.AiCapabilityRegistry capabilityRegistry,
        ILogger<AIPipeline> logger)
    {
        _contextEngine = contextEngine;
        _promptBuilder = promptBuilder;
        _router = router;
        _draftEngine = draftEngine;
        _citationVerifier = citationVerifier;
        _evaluator = evaluator;
        _securityLayer = securityLayer;
        _telemetryService = telemetryService;
        _workflowPlanner = workflowPlanner;
        _capabilityPlanner = capabilityPlanner;
        _capabilityRegistry = capabilityRegistry;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Guid caseId, string userInput, string taskType, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var context = new AIPipelineContext();

        // === STEP 1: Intent Classification ===
        context.Intent = taskType;
        context.Complexity = ClassifyComplexity(taskType, userInput);
        _logger.LogInformation("[Pipeline] Starting. Intent={Intent}, Complexity={Complexity}, CaseId={CaseId}", context.Intent, context.Complexity, caseId);

        // === STEP 1.5: Security Layer ===
        var securityResult = await _securityLayer.AssessAndSanitizeAsync(userInput, cancellationToken);
        if (securityResult.IsBlocked)
        {
            _logger.LogWarning("[Pipeline] Security Blocked. CaseId={CaseId}, Reason={Reason}", caseId, securityResult.FlagReason);
            return $"[SECURITY ALERT] Request was blocked by the AI Security Layer. Reason: {securityResult.FlagReason}";
        }
        userInput = securityResult.SanitizedInput;

        // === STEP 1.8: Workflow & Capability Planning (MCP) ===
        var workflow = _workflowPlanner.PlanWorkflow(userInput);
        var modelCap = _capabilityRegistry.GetCapability("nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free");
        var availableSkills = _capabilityPlanner.SelectSkillsForWorkflow(workflow, modelCap);
        _logger.LogInformation("[Pipeline] Workflow={Workflow}, ModelToolCalling={ToolCalling}, SelectedSkills={Skills}", workflow, modelCap.ToolCalling, string.Join(", ", availableSkills.Select(s => s.Name)));

        // === STEP 2: Context Engine (Memory + Retrieval) ===
        var contextXml = await BuildContextAsync(caseId, taskType, userInput, parameters, cancellationToken);
        context.CaseMemoryXml = contextXml;
        _logger.LogInformation("[Pipeline] Context assembled. Size={Chars} chars", contextXml.Length);

        // === STEP 3: Prompt Builder ===
        var templateName = ResolveTemplate(taskType, parameters);
        var promptVersion = _promptBuilder.GetTemplateVersion(templateName);
        var variables = new Dictionary<string, string> { { "CONTEXT", context.CaseMemoryXml } };
        context.SystemPrompt = _promptBuilder.BuildSystemPrompt(templateName, variables);
        context.FinalUserPrompt = userInput;
        _logger.LogInformation("[Pipeline] Prompt built. Template={Template} v{Version}", templateName, promptVersion);

        // === STEP 4: AI Router / Draft Engine ===
        string response;
        if (taskType == "LegalDraft")
        {
            var docType = parameters != null && parameters.ContainsKey("DocumentType") ? parameters["DocumentType"]?.ToString() ?? "Document" : "Document";
            response = await _draftEngine.DraftDocumentAsync(caseId, docType, context.FinalUserPrompt, context.CaseMemoryXml, cancellationToken);
        }
        else
        {
            context.ModelUsed = context.Complexity == "High" ? "DEEP" : "FAST";
            response = await _router.CompleteAsync(context.SystemPrompt, context.FinalUserPrompt, taskType, cancellationToken);
        }

        // === STEP 5: Citation Verification ===
        response = await _citationVerifier.VerifyCitationsAsync(response, cancellationToken);

        sw.Stop();
        var elapsedMs = sw.ElapsedMilliseconds;
        
        // === STEP 6: Telemetry & Evaluation ===
        // Fire and forget evaluation so we don't block the response to the user
        _ = Task.Run(async () =>
        {
            try
            {
                var evalResult = await _evaluator.EvaluateResponseAsync(context.SystemPrompt, context.FinalUserPrompt, response, elapsedMs);
                var log = new Clausio.Legal.Core.Entities.AI.AiTelemetryLog
                {
                    CaseId = caseId,
                    Intent = context.Intent,
                    PromptName = templateName,
                    Provider = "OpenRouter", // Default or fetch from router
                    Model = context.ModelUsed,
                    RouterDecision = context.Complexity,
                    LatencyMs = elapsedMs,
                    TokensIn = context.SystemPrompt.Length / 4 + context.FinalUserPrompt.Length / 4,
                    TokensOut = response.Length / 4,
                    RetrievalScore = evalResult.RetrievalQualityScore,
                    CitationConfidenceScore = evalResult.CitationConfidenceScore,
                    DraftScore = evalResult.DraftQualityScore,
                    HallucinationRiskScore = evalResult.HallucinationRiskScore,
                    TokenEfficiencyScore = evalResult.TokenEfficiencyScore,
                    IsSuccess = true
                };
                await _telemetryService.LogInteractionAsync(log, default);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AIPipeline] Evaluation failed.");
            }
        });

        _logger.LogInformation("[Pipeline] Completed. TotalMs={Ms}, Intent={Intent}", elapsedMs, context.Intent);

        return response;
    }

    public async IAsyncEnumerable<string> StreamExecuteAsync(Guid caseId, string userInput, string taskType, Dictionary<string, object>? parameters = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[Pipeline:Stream] Starting. Intent={Intent}, CaseId={CaseId}", taskType, caseId);

        // === Progress: Phase 1 ===
        yield return FormatProgressChunk("Understanding request...");

        // === STEP 1: Intent Classification ===
        var complexity = ClassifyComplexity(taskType, userInput);

        // === STEP 1.5: Security Layer ===
        var securityResult = await _securityLayer.AssessAndSanitizeAsync(userInput, cancellationToken);
        if (securityResult.IsBlocked)
        {
            _logger.LogWarning("[Pipeline:Stream] Security Blocked. CaseId={CaseId}, Reason={Reason}", caseId, securityResult.FlagReason);
            yield return $"[SECURITY ALERT] Request blocked: {securityResult.FlagReason}";
            yield break;
        }
        userInput = securityResult.SanitizedInput;

        // === Progress: Phase 2 ===
        yield return FormatProgressChunk("Loading case memory...");

        // === STEP 2: Context Engine ===
        var contextXml = await BuildContextAsync(caseId, taskType, userInput, parameters, cancellationToken);
        _logger.LogInformation("[Pipeline:Stream] Context assembled. Size={Chars} chars", contextXml.Length);

        // === Progress: Phase 3 ===
        yield return FormatProgressChunk("Retrieving legal evidence...");
        await Task.Delay(50, cancellationToken); // Simulate retrieval step display

        // === STEP 3: Prompt Builder ===
        var templateName = ResolveTemplate(taskType, parameters);
        var variables = new Dictionary<string, string> { { "CONTEXT", contextXml } };
        var systemPrompt = _promptBuilder.BuildSystemPrompt(templateName, variables);
        _logger.LogInformation("[Pipeline:Stream] Prompt built. Template={Template}", templateName);

        // === Progress: Phase 4 ===
        if (taskType == "LegalDraft")
            yield return FormatProgressChunk("Drafting document...");
        else
            yield return FormatProgressChunk("Generating response...");

        // === STEP 4: Stream from AI Router ===
        await foreach (var chunk in _router.StreamCompleteAsync(systemPrompt, userInput, taskType, cancellationToken))
        {
            yield return chunk;
        }

        sw.Stop();
        _logger.LogInformation("[Pipeline:Stream] Completed. TotalMs={Ms}", sw.ElapsedMilliseconds);
    }

    // ===================== Helpers =====================

    private async Task<string> BuildContextAsync(Guid caseId, string taskType, string userInput, Dictionary<string, object>? parameters, CancellationToken cancellationToken)
    {
        if (taskType == "LegalDraft")
        {
            var docType = parameters != null && parameters.ContainsKey("DocumentType") ? parameters["DocumentType"]?.ToString() : "Document";
            return await _contextEngine.BuildDraftingContextAsync(caseId, docType ?? "Document", userInput, cancellationToken);
        }
        else if (taskType == "Analysis" || taskType == "Summarization" || taskType == "ActionPlan")
        {
            return await _contextEngine.BuildAnalysisContextAsync(caseId, taskType, cancellationToken);
        }
        else
        {
            return await _contextEngine.BuildChatContextAsync(caseId, userInput, cancellationToken);
        }
    }

    private static string ClassifyComplexity(string taskType, string userInput)
    {
        int score = 0;
        var lowerTask = taskType.ToLowerInvariant();
        if (lowerTask.Contains("draft") || lowerTask.Contains("research") || lowerTask.Contains("actionplan") || lowerTask.Contains("contradiction")) score += 50;
        else if (lowerTask.Contains("analysis") || lowerTask.Contains("summarization")) score += 30;

        if (userInput.Length > 2500) score += 35;
        else if (userInput.Length > 800) score += 15;

        var keywords = new[] { "supreme court", "high court", "section", "article", "ipc", "crpc", "cpc", "statute", "precedent", "ratio decidendi" };
        var lowerInput = userInput.ToLowerInvariant();
        foreach (var kw in keywords)
        {
            if (lowerInput.Contains(kw)) score += 5;
        }

        if (score >= 60) return "High";
        if (score >= 30) return "Medium";
        return "Low";
    }

    private static string ResolveTemplate(string taskType, Dictionary<string, object>? parameters)
    {
        if (taskType == "LegalDraft")
        {
            var docType = parameters != null && parameters.ContainsKey("DocumentType") ? parameters["DocumentType"]?.ToString() : null;
            return docType switch
            {
                "Legal Notice" => "Drafts/LegalNotice",
                "Consumer Complaint" => "Drafts/ConsumerComplaint",
                "Agreement" => "Drafts/Agreement",
                "Employment Agreement" => "Drafts/EmploymentAgreement",
                "Affidavit" => "Drafts/Affidavit",
                _ => "LegalDraft"
            };
        }

        if (taskType == "Analysis")
        {
            return "Analysis/LegalReasoning";
        }
        
        if (taskType == "ActionPlan")
        {
            return "Analysis/RiskAssessment"; // Specialized action plan
        }

        return taskType switch
        {
            "Summarization" => "Analysis",
            _ => "GeneralChat"
        };
    }

    /// <summary>
    /// Format a progress chunk so the frontend can distinguish it from LLM content.
    /// Prefix: "[sys]" — frontend should render these as status messages, not as answer text.
    /// </summary>
    private static string FormatProgressChunk(string message)
        => $"[sys]{message}";
}
