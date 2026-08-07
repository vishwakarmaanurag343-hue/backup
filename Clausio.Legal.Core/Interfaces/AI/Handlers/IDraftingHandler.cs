using System;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Dtos;

namespace Clausio.Legal.Core.Interfaces.AI.Handlers;

public interface IDraftingHandler
{
    Task<string> HandleDocumentAsync(Guid caseId, DraftRequestDto request, CancellationToken cancellationToken = default);
    Task<string> HandleWhatsAppAsync(Guid caseId, WhatsAppRequestDto request, CancellationToken cancellationToken = default);
}
