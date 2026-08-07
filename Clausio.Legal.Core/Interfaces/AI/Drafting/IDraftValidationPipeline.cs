using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.AI.Drafting;

public interface IDraftValidationPipeline
{
    Task<(bool Passed, int Score, string Recommendation, string Feedback)> ValidateDraftAsync(
        string draftContent, 
        string documentType, 
        CancellationToken cancellationToken = default);
}
