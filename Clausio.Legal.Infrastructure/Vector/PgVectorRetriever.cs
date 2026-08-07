using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Clausio.Legal.Infrastructure.Vector;

public class PgVectorRetriever : IRetriever
{
    private readonly ClausioDbContext _dbContext;

    public PgVectorRetriever(ClausioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DocumentChunk>> RetrieveAsync(float[] queryEmbedding, Guid caseId, int topK = 10, CancellationToken cancellationToken = default)
    {
        var pgVector = new Pgvector.Vector(queryEmbedding);

        // Filter by CaseId, then sort by L2 distance
        var chunks = await _dbContext.DocumentChunks
            .Where(c => c.CaseId == caseId && c.Embedding != null)
            .OrderBy(c => c.Embedding!.L2Distance(pgVector))
            .Take(topK)
            .ToListAsync(cancellationToken);

        return chunks;
    }

    public async Task<List<DocumentChunk>> GetAllChunksForCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DocumentChunks
            .Where(c => c.CaseId == caseId)
            .ToListAsync(cancellationToken);
    }

    public async Task StoreChunksAsync(List<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
