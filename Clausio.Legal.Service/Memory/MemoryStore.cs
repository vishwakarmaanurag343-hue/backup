using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities.Memory;
using Clausio.Legal.Core.Interfaces.Memory;
using Clausio.Legal.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.Memory;

public class MemoryStore : IMemoryStore
{
    private readonly ClausioDbContext _db;
    private readonly ILogger<MemoryStore> _logger;

    public MemoryStore(ClausioDbContext db, ILogger<MemoryStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CaseMemory?> GetCaseMemoryAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        return await _db.CaseMemories.FirstOrDefaultAsync(m => m.CaseId == caseId, cancellationToken);
    }

    public async Task UpsertCaseMemoryAsync(CaseMemory memory, CancellationToken cancellationToken = default)
    {
        var existing = await _db.CaseMemories.FirstOrDefaultAsync(m => m.CaseId == memory.CaseId, cancellationToken);
        if (existing == null)
        {
            await _db.CaseMemories.AddAsync(memory, cancellationToken);
        }
        else
        {
            existing.CaseTitle = memory.CaseTitle;
            existing.CaseType = memory.CaseType;
            existing.ShortSummary = memory.ShortSummary;
            existing.CurrentStatus = memory.CurrentStatus;
            existing.KeyFacts = memory.KeyFacts;
            existing.ImportantDates = memory.ImportantDates;
            existing.Parties = memory.Parties;
            existing.LegalIssues = memory.LegalIssues;
            existing.CurrentObjective = memory.CurrentObjective;
            existing.LastUpdated = DateTime.UtcNow;
            _db.CaseMemories.Update(existing);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ConversationMemory>> GetRecentConversationsAsync(Guid caseId, int limit = 5, CancellationToken cancellationToken = default)
    {
        return await _db.ConversationMemories
            .Where(m => m.CaseId == caseId)
            .OrderByDescending(m => m.LastUpdated)
            .Take(limit)
            .OrderBy(m => m.LastUpdated) // Return in chronological order
            .ToListAsync(cancellationToken);
    }

    public async Task AddConversationAsync(ConversationMemory memory, CancellationToken cancellationToken = default)
    {
        await _db.ConversationMemories.AddAsync(memory, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<DraftMemory>> GetRecentDraftsAsync(Guid caseId, int limit = 5, CancellationToken cancellationToken = default)
    {
        return await _db.DraftMemories
            .Where(m => m.CaseId == caseId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddDraftMemoryAsync(DraftMemory memory, CancellationToken cancellationToken = default)
    {
        await _db.DraftMemories.AddAsync(memory, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
