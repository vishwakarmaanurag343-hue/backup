using System;
using System.Collections.Generic;
using Clausio.Legal.Core.Entities;

namespace Clausio.Legal.Core.Interfaces.Retrieval;

public interface IChunkProcessor
{
    List<DocumentChunk> Process(string text, Guid documentId, Guid caseId, string? documentType);
}
