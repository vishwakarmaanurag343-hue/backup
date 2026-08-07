using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Clausio.Legal.Service.DocumentIntelligence;

public class TableExtractor
{
    private static readonly Regex TableRowRegex = new Regex(@"\|.*?\|", RegexOptions.Multiline);

    public (string ProcessedText, int TableCount) Extract(string text)
    {
        var matches = TableRowRegex.Matches(text);
        
        // Very rudimentary count: if we see multiple table rows, we count them as tables.
        // A robust system groups contiguous rows into single tables.
        int rowCount = matches.Count;
        int estimatedTables = rowCount > 1 ? 1 : 0; 
        
        return (text, estimatedTables);
    }
}
