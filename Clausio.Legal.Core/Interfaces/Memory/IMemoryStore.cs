using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities.Memory;

namespace Clausio.Legal.Core.Interfaces.Memory;

public interface IMemoryStore
{
    Task<CaseMemory?> GetCaseMemoryAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task UpsertCaseMemoryAsync(CaseMemory memory, CancellationToken cancellationToken = default);
    
    Task<List<ConversationMemory>> GetRecentConversationsAsync(Guid caseId, int limit = 5, CancellationToken cancellationToken = default);
    Task AddConversationAsync(ConversationMemory memory, CancellationToken cancellationToken = default);
    
    Task<List<DraftMemory>> GetRecentDraftsAsync(Guid caseId, int limit = 5, CancellationToken cancellationToken = default);
    Task AddDraftMemoryAsync(DraftMemory memory, CancellationToken cancellationToken = default);
}
