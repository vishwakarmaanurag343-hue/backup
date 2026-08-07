using System;

namespace Clausio.Legal.Core.Entities.Memory;

public class CaseMemory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    
    public string CaseTitle { get; set; }
    public string CaseType { get; set; }
    public string ShortSummary { get; set; }
    public string CurrentStatus { get; set; }
    public string KeyFacts { get; set; }
    public string ImportantDates { get; set; }
    public string Parties { get; set; }
    public string LegalIssues { get; set; }
    public string CurrentObjective { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
