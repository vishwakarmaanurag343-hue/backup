using System.Collections.Generic;
using System.Text.RegularExpressions;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.Retrieval.Citation;

public class CitationExtractor : ICitationExtractor
{
    private readonly ILogger<CitationExtractor> _logger;

    public CitationExtractor(ILogger<CitationExtractor> logger)
    {
        _logger = logger;
    }

    public List<string> ExtractCitations(string text)
    {
        var citations = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return citations;

        // Basic regex for legal citations (e.g. Sections, Acts, v. judgments)
        var sectionActRegex = new Regex(@"(Section\s+\d+[A-Z]?\s+of\s+the\s+[A-Za-z\s]+Act(,\s+\d{4})?)", RegexOptions.IgnoreCase);
        var judgmentRegex = new Regex(@"([A-Za-z\s]+v\.\s+[A-Za-z\s]+)", RegexOptions.IgnoreCase);

        foreach (Match match in sectionActRegex.Matches(text))
        {
            citations.Add(match.Value.Trim());
        }

        foreach (Match match in judgmentRegex.Matches(text))
        {
            citations.Add(match.Value.Trim());
        }

        return citations;
    }
}
