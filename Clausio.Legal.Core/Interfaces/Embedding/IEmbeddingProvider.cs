using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.Embedding;

public interface IEmbeddingProvider
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default);
}
