using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.AI;

public interface ILLMProvider
{
    Task<string> CompleteAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamCompleteAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
