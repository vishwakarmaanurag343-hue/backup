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
        
        if (!citations.Any())
        {
            // No citations to verify
            return true;
        }

        var contextText = string.Join(" ", retrievedContext.Select(c => c.TextContent)).ToLowerInvariant();
        var allVerified = true;

        foreach (var citation in citations)
        {
            // Very naive verification: does the exact citation string exist in the retrieved text?
            // In a production system, this would use semantic similarity or LLM-based verification.
            if (!contextText.Contains(citation.ToLowerInvariant()))
            {
                _logger.LogWarning("Verification failed for citation: {Citation}", citation);
                allVerified = false;
            }
        }

        return allVerified;
    }
}
