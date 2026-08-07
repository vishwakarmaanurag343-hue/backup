using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.AI.Validation;

public interface ICitationVerifier
{
    Task<string> VerifyCitationsAsync(string aiResponse, CancellationToken cancellationToken = default);
}
