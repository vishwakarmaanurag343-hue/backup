using System;

namespace Clausio.Legal.Core.Entities.Memory;

public class ConversationMemory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    
    public string ConversationSummary { get; set; }
    public string ImportantDecisions { get; set; }
    public string PreviousAiSuggestions { get; set; }
    public string PendingTasks { get; set; }
    
    public int MessageCountSinceLastSummary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
