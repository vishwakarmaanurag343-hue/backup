using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities;

namespace Clausio.Legal.Core.Interfaces.Retrieval;

/// <summary>
/// BM25 lexical retriever for keyword-based legal text search.
/// Complements vector search in the hybrid retrieval pipeline.
/// </summary>
public interface IBM25Retriever
{
    /// <summary>
    /// Build BM25 index over a set of chunks (typically pre-fetched from the DB for the case).
    /// </summary>
    void BuildIndex(List<DocumentChunk> chunks);

    /// <summary>
    /// Search the in-memory BM25 index for the top-K chunks matching the query.
    /// </summary>
    List<(DocumentChunk Chunk, double Score)> Search(string query, int topK = 15);
}

/// <summary>
/// Hybrid retriever that fuses BM25 (lexical) and Vector (semantic) results using
/// Reciprocal Rank Fusion (RRF), producing a single ranked list.
/// </summary>
public interface IHybridRetriever
{
    Task<List<(DocumentChunk Chunk, double FusedScore)>> RetrieveAsync(
        string query,
        Guid caseId,
        int topK = 10,
        CancellationToken cancellationToken = default);
}
