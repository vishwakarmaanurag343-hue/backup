using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities.AI;

namespace Clausio.Legal.Core.Interfaces.AI.Evaluation;

public interface ITelemetryService
{
    Task LogInteractionAsync(AiTelemetryLog log, CancellationToken cancellationToken = default);
}
