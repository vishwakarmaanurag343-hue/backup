using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.AI.Drafting;

public interface IDraftEngine
{
    Task<string> DraftDocumentAsync(Guid caseId, string documentType, string instructions, string contextXml, CancellationToken cancellationToken = default);
}
