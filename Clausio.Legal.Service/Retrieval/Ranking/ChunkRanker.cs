using System;
using System.Collections.Generic;
using System.Linq;
using Clausio.Legal.Core.Entities;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.Retrieval.Ranking;

public class ChunkRanker : IChunkRanker
{
    private readonly ILogger<ChunkRanker> _logger;

    public ChunkRanker(ILogger<ChunkRanker> logger)
    {
        _logger = logger;
    }

    public List<DocumentChunk> Rank(List<DocumentChunk> chunks, Guid currentCaseId)
    {
        if (chunks == null || !chunks.Any())
            return new List<DocumentChunk>();

        _logger.LogInformation("[ChunkRanker] Ranking {Count} chunks for case {CaseId}", chunks.Count, currentCaseId);

        // 1. Deduplication (naive textual similarity based on Levenshtein/Length ratio, or exact match)
        var deduplicated = DeduplicateChunks(chunks);
        _logger.LogInformation("[ChunkRanker] Deduplication removed {RemovedCount} duplicate/near-duplicate chunks.", chunks.Count - deduplicated.Count);

        // 2. Ranking
        // Priority 1: Exact Case Match
        // Priority 2: Document Type Metadata priority
        return deduplicated
            .OrderByDescending(c => c.CaseId == currentCaseId)
            .ThenByDescending(c => GetDocumentPriority(c.DocumentType))
            .ToList();
    }

    private List<DocumentChunk> DeduplicateChunks(List<DocumentChunk> chunks)
    {
        var result = new List<DocumentChunk>();
        var seenTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in chunks)
        {
            var textPreview = GetNormalizedPreview(chunk.TextContent);
            if (!seenTexts.Contains(textPreview))
            {
                seenTexts.Add(textPreview);
                result.Add(chunk);
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a normalized string representation of the first ~100 characters to detect obvious duplicates.
    /// In a production system, this would use SimHash or MinHash.
    /// </summary>
    private string GetNormalizedPreview(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var normalized = new string(text.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToLowerInvariant();
        return normalized.Length > 100 ? normalized.Substring(0, 100) : normalized;
    }

    private int GetDocumentPriority(string? documentType)
    {
        if (string.IsNullOrEmpty(documentType)) return 0;
        
        var lower = documentType.ToLowerInvariant();
        
        // High priority legal metadata
        if (lower.Contains("judgment") || lower.Contains("order") || lower.Contains("decree")) return 3;
        if (lower.Contains("precedent") || lower.Contains("statute") || lower.Contains("act")) return 2;
        if (lower.Contains("template") || lower.Contains("agreement") || lower.Contains("contract")) return 1;
        
        return 0;
    }
}
