using Clausio.Legal.Core.Entities;
using Clausio.Legal.Core.Entities.AI;
using Microsoft.EntityFrameworkCore;

namespace Clausio.Legal.Infrastructure;

public class ClausioDbContext(DbContextOptions<ClausioDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Case> Cases => Set<Case>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ActionPlan> ActionPlans => Set<ActionPlan>();
    public DbSet<Contradiction> Contradictions => Set<Contradiction>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Hearing> Hearings => Set<Hearing>();
    public DbSet<HearingOrder> HearingOrders => Set<HearingOrder>();
    public DbSet<LegalResearch> LegalResearches => Set<LegalResearch>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();
    public DbSet<Readiness> Readinesses => Set<Readiness>();
    public DbSet<ReadinessChecklistItem> ReadinessChecklistItems => Set<ReadinessChecklistItem>();

    // Phase 1 Memory & Context Intelligence
    public DbSet<Clausio.Legal.Core.Entities.Memory.CaseMemory> CaseMemories => Set<Clausio.Legal.Core.Entities.Memory.CaseMemory>();
    public DbSet<Clausio.Legal.Core.Entities.Memory.ConversationMemory> ConversationMemories => Set<Clausio.Legal.Core.Entities.Memory.ConversationMemory>();
    public DbSet<Clausio.Legal.Core.Entities.Memory.DraftMemory> DraftMemories => Set<Clausio.Legal.Core.Entities.Memory.DraftMemory>();
    public DbSet<Clausio.Legal.Core.Entities.Memory.UserPreferences> UserPreferences => Set<Clausio.Legal.Core.Entities.Memory.UserPreferences>();
    // Phase 2 RAG Foundation
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    // AI Analytics & Telemetry
    public DbSet<AiTelemetryLog> AiTelemetryLogs => Set<AiTelemetryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClausioDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        TouchTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void TouchTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
