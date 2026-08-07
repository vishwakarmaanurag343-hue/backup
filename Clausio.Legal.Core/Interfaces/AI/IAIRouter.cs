using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.AI;

public interface IAIRouter
{
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string promptType = "chat",
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamCompleteAsync(
        string systemPrompt,
        string userPrompt,
        string promptType = "chat",
        CancellationToken cancellationToken = default);
}
