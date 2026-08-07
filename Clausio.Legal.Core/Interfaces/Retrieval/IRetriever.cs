using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities;

namespace Clausio.Legal.Core.Interfaces.Retrieval;

public interface IRetriever
{
    Task<List<DocumentChunk>> RetrieveAsync(float[] queryEmbedding, Guid caseId, int topK = 10, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> GetAllChunksForCaseAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task StoreChunksAsync(List<DocumentChunk> chunks, CancellationToken cancellationToken = default);
}
