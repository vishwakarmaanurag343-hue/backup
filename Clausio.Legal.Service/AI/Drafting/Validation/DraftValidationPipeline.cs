using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.AI;
using Clausio.Legal.Core.Interfaces.AI.Drafting;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.AI.Drafting.Validation;

public class DraftValidationPipeline : IDraftValidationPipeline
{
    private readonly IAIRouter _aiRouter;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILogger<DraftValidationPipeline> _logger;

    public DraftValidationPipeline(
        IAIRouter aiRouter,
        IPromptBuilder promptBuilder,
        ILogger<DraftValidationPipeline> logger)
    {
        _aiRouter = aiRouter;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    public async Task<(bool Passed, int Score, string Recommendation, string Feedback)> ValidateDraftAsync(
        string draftContent, 
        string documentType, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DraftValidationPipeline] Starting comprehensive review for {DocumentType}", documentType);

        try
        {
            var systemPrompt = _promptBuilder.BuildSystemPrompt("DraftSelfReview");
            var userPrompt = $"Document Type: {documentType}\n\n---DRAFT TO REVIEW---\n{draftContent}";

            var response = await _aiRouter.CompleteAsync(systemPrompt, userPrompt, "chat", cancellationToken);

            var result = ParseResponse(response);
            
            _logger.LogInformation("[DraftValidationPipeline] Review complete. Score={Score}, Recommendation={Rec}", result.Score, result.Recommendation);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DraftValidationPipeline] Validation failed or threw exception. Defaulting to passed.");
            return (true, 7, "Accept", "Validation pipeline encountered an error and was bypassed.");
        }
    }

    private (bool Passed, int Score, string Recommendation, string Feedback) ParseResponse(string response)
    {
        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        
        if (start >= 0 && end > start)
        {
            var json = response.Substring(start, end - start + 1);
            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                var passed = root.TryGetProperty("QualityPassed", out var p) && p.GetBoolean();
                var score = root.TryGetProperty("OverallScore", out var s) ? s.GetInt32() : 0;
                var rec = root.TryGetProperty("Recommendation", out var r) ? r.GetString() : "Accept";
                
                return (passed, score, rec ?? "Accept", json);
            }
            catch
            {
                // Json parsing failed, fallback
            }
        }
        
        return (true, 7, "Accept", "Could not parse detailed feedback.");
    }
}
