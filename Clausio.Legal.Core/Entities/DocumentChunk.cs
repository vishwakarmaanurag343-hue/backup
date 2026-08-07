using System;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace Clausio.Legal.Core.Entities;

public class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid CaseId { get; set; }
    
    public int? PageNumber { get; set; }
    public string? Section { get; set; }
    public string? Heading { get; set; }
    
    public required string TextContent { get; set; }
    
    [Column(TypeName = "vector(1536)")]
    public Pgvector.Vector? Embedding { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
    
    public string? DocumentType { get; set; }
}
