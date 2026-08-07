using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.AI.Research;

public interface IDeepResearchPipeline
{
    Task<string> ExecuteResearchAsync(Guid caseId, string researchQuery, CancellationToken cancellationToken = default);
}
