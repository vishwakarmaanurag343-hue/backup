using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities;
using Clausio.Legal.Core.Interfaces.Embedding;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.Retrieval.Hybrid;

public class HybridRetriever : IHybridRetriever
{
    private readonly IRetriever _vectorRetriever;
    private readonly IBM25Retriever _bm25Retriever;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ILogger<HybridRetriever> _logger;
    private const int RRF_K = 60; // Standard constant for Reciprocal Rank Fusion

    public HybridRetriever(
        IRetriever vectorRetriever,
        IBM25Retriever bm25Retriever,
        IEmbeddingProvider embeddingProvider,
        ILogger<HybridRetriever> logger)
    {
        _vectorRetriever = vectorRetriever;
        _bm25Retriever = bm25Retriever;
        _embeddingProvider = embeddingProvider;
        _logger = logger;
    }

    public async Task<List<(DocumentChunk Chunk, double FusedScore)>> RetrieveAsync(
        string query,
        Guid caseId,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[HybridRetriever] Starting hybrid retrieval for CaseId: {CaseId}, Query: {Query}", caseId, query);

        try
        {
            // 1. Fetch Vector candidates
            List<DocumentChunk> vectorCandidates = new();
            try
            {
                var queryEmbedding = await _embeddingProvider.GenerateEmbeddingAsync(query, cancellationToken);
                if (queryEmbedding != null && queryEmbedding.Length > 0)
                {
                    vectorCandidates = await _vectorRetriever.RetrieveAsync(queryEmbedding, caseId, topK: 30, cancellationToken);
                }
            }
            catch (Exception embEx)
            {
                _logger.LogWarning(embEx, "[HybridRetriever] Vector embedding retrieval failed. Proceeding with BM25 keyword retrieval.");
            }

            // 2. Fetch BM25 candidates
            var allCaseChunks = await _vectorRetriever.GetAllChunksForCaseAsync(caseId, cancellationToken);
            _bm25Retriever.BuildIndex(allCaseChunks);
            var bm25Candidates = _bm25Retriever.Search(query, topK: 30);

            // 3. Reciprocal Rank Fusion (RRF)
            var fusionScores = new Dictionary<Guid, (DocumentChunk Chunk, double Score)>();

            for (int i = 0; i < vectorCandidates.Count; i++)
            {
                var chunk = vectorCandidates[i];
                var rrfScore = 1.0 / (RRF_K + i + 1);
                if (!fusionScores.ContainsKey(chunk.Id)) fusionScores[chunk.Id] = (chunk, 0);
                fusionScores[chunk.Id] = (chunk, fusionScores[chunk.Id].Score + rrfScore);
            }

            for (int i = 0; i < bm25Candidates.Count; i++)
            {
                var chunk = bm25Candidates[i].Chunk;
                var rrfScore = 1.0 / (RRF_K + i + 1);
                if (!fusionScores.ContainsKey(chunk.Id)) fusionScores[chunk.Id] = (chunk, 0);
                fusionScores[chunk.Id] = (chunk, fusionScores[chunk.Id].Score + rrfScore);
            }

            // 4. Sort and take top K
            var finalRanked = fusionScores.Values
                .OrderByDescending(v => v.Score)
                .Take(topK)
                .ToList();

            _logger.LogInformation("[HybridRetriever] Fusion complete. Returned {Count} chunks.", finalRanked.Count);
            return finalRanked;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[HybridRetriever] Retrieval failed. Gracefully returning empty context.");
            return new List<(DocumentChunk Chunk, double FusedScore)>();
        }
    }
}
