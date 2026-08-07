using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Clausio.MCP.Session;

public class McpSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    
    public ConcurrentDictionary<string, object> State { get; set; } = new();
    public ConcurrentDictionary<string, string> ToolCache { get; set; } = new();

    public McpSession(Guid caseId, Guid userId = default, Guid tenantId = default)
    {
        CaseId = caseId;
        UserId = userId;
        TenantId = tenantId;
    }
}
