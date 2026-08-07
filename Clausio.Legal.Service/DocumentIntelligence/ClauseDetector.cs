using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Clausio.Legal.Service.DocumentIntelligence;

public class ClauseDetector
{
    private static readonly Regex ClauseRegex = new Regex(@"^(Clause\s+\d+|Article\s+\d+|Section\s+\d+|\d+\.)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public (string ProcessedText, List<string> Clauses) Detect(string text)
    {
        var matches = ClauseRegex.Matches(text);
        var clauses = new List<string>();

        foreach (Match match in matches)
        {
            if (!clauses.Contains(match.Value.Trim()))
            {
                clauses.Add(match.Value.Trim());
            }
        }

        return (text, clauses);
    }
}
