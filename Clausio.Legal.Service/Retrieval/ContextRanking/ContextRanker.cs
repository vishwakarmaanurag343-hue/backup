using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.Retrieval.ContextRanking;

public class ContextRanker : IContextRanker
{
    private readonly ILogger<ContextRanker> _logger;

    public ContextRanker(ILogger<ContextRanker> logger)
    {
        _logger = logger;
    }

    public Task<string> ScoreRankAndCompressAsync(string rawContext, int maxTokens = 1500)
    {
        _logger.LogInformation("[ContextRanker] Starting context compression. Original length: {Length} chars", rawContext.Length);

        // 1. Remove duplicate lines or highly similar paragraphs
        var deduplicated = DeduplicateText(rawContext);

        // 2. Compress (remove unnecessary whitespace, filler words if aggressive compression is needed)
        var compressed = Regex.Replace(deduplicated, @"\n{3,}", "\n\n");
        compressed = Regex.Replace(compressed, @"\s{2,}", " ");

        // 3. Truncate to max tokens (approx 4 chars per token)
        var maxChars = maxTokens * 4;
        if (compressed.Length > maxChars)
        {
            _logger.LogWarning("[ContextRanker] Context exceeds max tokens. Truncating from {Length} to {MaxChars} chars.", compressed.Length, maxChars);
            compressed = compressed.Substring(0, maxChars) + "\n...[Context Truncated]...";
        }

        _logger.LogInformation("[ContextRanker] Compression complete. Final length: {Length} chars", compressed.Length);
        
        return Task.FromResult(compressed);
    }

    private string DeduplicateText(string text)
    {
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var seenLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            var normalizedLine = line.Trim();
            if (normalizedLine.Length < 20)
            {
                // Keep short structural lines like <case_context>
                result.AppendLine(line);
                continue;
            }

            if (seenLines.Add(normalizedLine))
            {
                result.AppendLine(line);
            }
        }

        return result.ToString();
    }
}
