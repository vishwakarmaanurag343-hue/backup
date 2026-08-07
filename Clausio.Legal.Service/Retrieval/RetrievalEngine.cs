using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities;
using Clausio.Legal.Core.Interfaces.Embedding;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.Retrieval;

public class RetrievalEngine : IRetrievalEngine
{
    private readonly ILogger<RetrievalEngine> _logger;
    private readonly IChunkProcessor _chunkProcessor;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IRetriever _retriever; // Kept for storing
    private readonly IHybridRetriever _hybridRetriever;
    private readonly IChunkRanker _chunkRanker;

    public RetrievalEngine(
        ILogger<RetrievalEngine> logger,
        IChunkProcessor chunkProcessor,
        IEmbeddingProvider embeddingProvider,
        IRetriever retriever,
        IHybridRetriever hybridRetriever,
        IChunkRanker chunkRanker)
    {
        _logger = logger;
        _chunkProcessor = chunkProcessor;
        _embeddingProvider = embeddingProvider;
        _retriever = retriever;
        _hybridRetriever = hybridRetriever;
        _chunkRanker = chunkRanker;
    }

    public async Task ProcessDocumentAsync(Guid documentId, Guid caseId, string text, string? documentType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing document {DocumentId} for case {CaseId}", documentId, caseId);

        // 1. Chunking
        var chunks = _chunkProcessor.Process(text, documentId, caseId, documentType);
        if (!chunks.Any())
        {
            _logger.LogWarning("No chunks generated for document {DocumentId}", documentId);
            return;
        }

        // 2. Embedding
        var textsToEmbed = chunks.Select(c => c.TextContent).ToList();
        var embeddings = await _embeddingProvider.GenerateEmbeddingsAsync(textsToEmbed, cancellationToken);

        for (int i = 0; i < chunks.Count; i++)
        {
            if (i < embeddings.Count)
            {
                chunks[i].Embedding = new Pgvector.Vector(embeddings[i]);
            }
        }

        // 3. Storage
        await _retriever.StoreChunksAsync(chunks, cancellationToken);
        _logger.LogInformation("Successfully stored {ChunkCount} chunks for document {DocumentId}", chunks.Count, documentId);
    }

    public async Task<List<DocumentChunk>> GetContextAsync(string query, Guid caseId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving context for query on case {CaseId}", caseId);

        // 1. Retrieve chunks via Hybrid Retrieval (BM25 + Vector + RRF)
        var hybridResults = await _hybridRetriever.RetrieveAsync(query, caseId, topK: 15, cancellationToken);
        var chunks = hybridResults.Select(r => r.Chunk).ToList();

        // 2. Post-retrieval ranking (Deduplication, Metadata prioritization)
        var rankedChunks = _chunkRanker.Rank(chunks, caseId);

        // Return top 5 optimized chunks
        return rankedChunks.Take(5).ToList();
    }
}
