using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities;

namespace Clausio.Legal.Core.Interfaces.Retrieval;

public interface IRetrievalEngine
{
    Task ProcessDocumentAsync(Guid documentId, Guid caseId, string text, string? documentType, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> GetContextAsync(string query, Guid caseId, CancellationToken cancellationToken = default);
}
