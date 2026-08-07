using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.AI.Pipeline;

public interface IAIPipeline
{
    Task<string> ExecuteAsync(Guid caseId, string userInput, string taskType, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    
    IAsyncEnumerable<string> StreamExecuteAsync(Guid caseId, string userInput, string taskType, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
}
