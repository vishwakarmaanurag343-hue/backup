using System;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Dtos;

namespace Clausio.Legal.Core.Interfaces.AI.Handlers;

public interface IChatHandler
{
    Task<string> HandleAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}
