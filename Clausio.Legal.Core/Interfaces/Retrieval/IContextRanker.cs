using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.Retrieval;

public interface IContextRanker
{
    Task<string> ScoreRankAndCompressAsync(string rawContext, int maxTokens = 1500);
}
