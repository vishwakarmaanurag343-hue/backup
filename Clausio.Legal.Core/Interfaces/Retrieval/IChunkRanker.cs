using System;
using System.Collections.Generic;
using Clausio.Legal.Core.Entities;

namespace Clausio.Legal.Core.Interfaces.Retrieval;

public interface IChunkRanker
{
    List<DocumentChunk> Rank(List<DocumentChunk> chunks, Guid currentCaseId);
}
