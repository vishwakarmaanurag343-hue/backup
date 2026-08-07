using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Clausio.Legal.Service.DocumentIntelligence;

public class LayoutAnalyzer
{
    // A rudimentary layout analyzer operating on plain text from OCR.
    // In a real vision-based OCR system, bounding boxes would determine this.
    public (string AnalyzedText, List<string> Headings) Analyze(string ocrText)
    {
        var lines = ocrText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var headings = new List<string>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // Heuristic for Heading: All CAPS, relatively short, or starts with Number/Article
            if (line.Length > 2 && line.Length < 100)
            {
                if (line == line.ToUpperInvariant() && !Regex.IsMatch(line, @"^[0-9\W]+$"))
                {
                    headings.Add(line);
                }
            }
        }

        return (ocrText, headings);
    }
}
