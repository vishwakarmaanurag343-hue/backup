using System;

namespace Clausio.Legal.Core.Entities.Memory;

public class DraftMemory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    
    public string DraftType { get; set; }
    public string DraftVersion { get; set; }
    public string DraftStatus { get; set; }
    public string LastDraftContent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
