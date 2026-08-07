using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.AI.Validation;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.AI.Validation;

/// <summary>
/// Citation structure extracted from AI text.
/// </summary>
public record ExtractedCitation(string Raw, string Type, string ActName, string SectionOrArticle);

public enum CitationConfidence
{
    Verified,
    LikelyVerified,
    Unverified
}

public class CitationVerifier : ICitationVerifier
{
    private readonly ILogger<CitationVerifier> _logger;

    // Known Indian statutes — in production this should be backed by a DB table
    private static readonly HashSet<string> KnownActs = new(StringComparer.OrdinalIgnoreCase)
    {
        "Indian Contract Act", "Indian Penal Code", "Code of Civil Procedure", "Code of Criminal Procedure",
        "Consumer Protection Act", "Transfer of Property Act", "Specific Relief Act",
        "Companies Act", "Income Tax Act", "GST Act", "Arbitration and Conciliation Act",
        "Negotiable Instruments Act", "Hindu Marriage Act", "Special Marriage Act",
        "Information Technology Act", "Motor Vehicles Act", "Indian Evidence Act",
        "Bharatiya Nyaya Sanhita", "Bharatiya Nagarik Suraksha Sanhita",
        "Bharatiya Sakshya Adhiniyam", "BNS", "BNSS", "BSA",
        "Constitution of India", "Prevention of Corruption Act", "POCSO Act",
        "Domestic Violence Act", "Maintenance and Welfare of Parents Act",
        "Real Estate Regulation Act", "RERA", "Insolvency and Bankruptcy Code", "IBC"
    };

    // Regex to match patterns like "Section 302 of the Indian Penal Code" or "Article 21 of the Constitution"
    private static readonly Regex CitationRegex = new(
        @"(Section|Article|Rule|Order|Schedule|Clause)\s+(\d+[A-Za-z]?(?:\s*[,&]\s*\d+[A-Za-z]?)*)\s+(?:of\s+)?(?:the\s+)?([A-Z][A-Za-z\s,]+?)(?:\s*,\s*\d{4})?(?=[,.\s]|$)",
        RegexOptions.Compiled | RegexOptions.Multiline
    );

    public CitationVerifier(ILogger<CitationVerifier> logger)
    {
        _logger = logger;
    }

    public Task<string> VerifyCitationsAsync(string aiResponse, CancellationToken cancellationToken = default)
    {
        var citations = ExtractCitations(aiResponse);

        if (citations.Count == 0)
            return Task.FromResult(aiResponse);

        var unverified = new List<string>();
        var likelyVerified = new List<string>();

        foreach (var citation in citations)
        {
            var confidence = AssessConfidence(citation.ActName);
            
            if (confidence == CitationConfidence.Unverified)
            {
                _logger.LogWarning("[CitationVerifier] Unverified citation: {Citation}", citation.Raw);
                unverified.Add(citation.Raw);
            }
            else if (confidence == CitationConfidence.LikelyVerified)
            {
                likelyVerified.Add(citation.Raw);
            }
        }

        if (unverified.Count > 0 || likelyVerified.Count > 0)
        {
            // If the response is a JSON string (e.g. structured case summary or draft JSON), do not append markdown onto JSON
            var trimmedResponse = aiResponse.Trim();
            if (trimmedResponse.StartsWith("{") && (trimmedResponse.EndsWith("}") || trimmedResponse.Contains("\"Case_ID\"") || trimmedResponse.Contains("\"DraftText\"")))
            {
                _logger.LogInformation("[CitationVerifier] Response is structured JSON. Skipping markdown disclaimer append to preserve JSON schema integrity.");
                return Task.FromResult(aiResponse);
            }

            var disclaimer = $"\n\n---\n> ⚠️ **Citation Verification Report:**\n";
            
            if (unverified.Count > 0)
            {
                disclaimer += "> **Unverified (Manual Check Required):**\n";
                foreach (var c in unverified) disclaimer += $"> - `{c}`\n";
            }
            
            if (likelyVerified.Count > 0)
            {
                disclaimer += "> **Likely Verified (Fuzzy Match):**\n";
                foreach (var c in likelyVerified) disclaimer += $"> - `{c}`\n";
            }
            
            disclaimer += "---";

            _logger.LogWarning("[CitationVerifier] Added verification notice for {UnverifiedCount} unverified and {LikelyCount} likely verified citations.", unverified.Count, likelyVerified.Count);
            return Task.FromResult(aiResponse + disclaimer);
        }

        _logger.LogInformation("[CitationVerifier] All {Count} citations verified perfectly.", citations.Count);
        return Task.FromResult(aiResponse);
    }

    private List<ExtractedCitation> ExtractCitations(string text)
    {
        var result = new List<ExtractedCitation>();
        var matches = CitationRegex.Matches(text);

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 4)
            {
                result.Add(new ExtractedCitation(
                    Raw: match.Value.Trim(),
                    Type: match.Groups[1].Value,
                    ActName: match.Groups[3].Value.Trim(),
                    SectionOrArticle: match.Groups[2].Value.Trim()
                ));
            }
        }

        return result;
    }

    private CitationConfidence AssessConfidence(string actName)
    {
        // 1. Exact Match
        if (KnownActs.Contains(actName))
            return CitationConfidence.Verified;

        var normalizedInput = Regex.Replace(actName, @"[^a-zA-Z0-9\s]", "").Trim().ToLowerInvariant();

        // 2. Fuzzy / Substring Match
        foreach (var known in KnownActs)
        {
            var normalizedKnown = Regex.Replace(known, @"[^a-zA-Z0-9\s]", "").ToLowerInvariant();
            
            // Substring checks
            if (normalizedInput.Contains(normalizedKnown) || normalizedKnown.Contains(normalizedInput))
            {
                return CitationConfidence.LikelyVerified;
            }
        }

        return CitationConfidence.Unverified;
    }
}
