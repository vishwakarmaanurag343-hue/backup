using System;
using System.Collections.Generic;
using System.Linq;
using Clausio.Legal.Core.Entities;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.Retrieval.Citation;

public class CitationVerifier : ICitationVerifier
{
    private readonly ILogger<CitationVerifier> _logger;
    private readonly ICitationExtractor _extractor;

    public CitationVerifier(ILogger<CitationVerifier> logger, ICitationExtractor extractor)
    {
        _logger = logger;
        _extractor = extractor;
    }

    public bool VerifyCitation(string generatedText, List<DocumentChunk> retrievedContext)
    {
        var citations = _extractor.ExtractCitations(generatedText);
        
        if (!citations.Any()) return true;

        var contextText = string.Join(" ", retrievedContext.Select(c => c.TextContent)).ToLowerInvariant();
        int validCount = 0;

        foreach (var citation in citations)
        {
            var lowerCitation = citation.ToLowerInvariant();
            int credibilityScore = EvaluateCredibility(lowerCitation, contextText);

            if (credibilityScore >= 50)
            {
                validCount++;
                _logger.LogInformation("[CitationVerifier] Verified Citation '{Citation}' with Credibility Score {Score}%", citation, credibilityScore);
            }
            else
            {
                _logger.LogWarning("[CitationVerifier] Citation '{Citation}' failed credibility check (Score {Score}%)", citation, credibilityScore);
            }
        }

        return (double)validCount / citations.Count >= 0.7;
    }

    private int EvaluateCredibility(string citation, string contextText)
    {
        int score = 0;

        // 1. Text Context Match
        if (contextText.Contains(citation)) score += 40;
        else if (contextText.Split(' ').Any(w => w.Length > 4 && citation.Contains(w))) score += 20;

        // 2. Recognized Statutory Act Validation
        var recognizedActs = new[] { "ipc", "crpc", "cpc", "constitution", "evidence act", "pocso", "ndps", "arbitration", "companies act", "gst act", "consumer protection" };
        if (recognizedActs.Any(act => citation.Contains(act))) score += 30;

        // 3. Judicial Authority Level Verification
        if (citation.Contains("supreme court") || citation.Contains("air ") || citation.Contains("scc")) score += 30;
        else if (citation.Contains("high court")) score += 20;

        return Math.Min(score, 100);
    }
}
