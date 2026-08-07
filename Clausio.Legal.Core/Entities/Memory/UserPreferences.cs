using System;

namespace Clausio.Legal.Core.Entities.Memory;

public class UserPreferences
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    
    public string PreferredLanguage { get; set; }
    public string WritingStyle { get; set; }
    public string CitationStyle { get; set; }
    public string PreferredJurisdiction { get; set; }
    public string DraftFormat { get; set; }
    public string SignatureFormat { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
