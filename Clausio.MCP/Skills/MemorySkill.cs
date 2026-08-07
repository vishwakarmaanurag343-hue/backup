using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.Memory;
using Clausio.MCP.Interfaces;
using Clausio.MCP.Session;
using Microsoft.Extensions.Logging;

namespace Clausio.MCP.Skills;

public class MemorySkill : IMcpSkill
{
    private readonly IMemoryStore _memoryStore;
    private readonly ILogger<MemorySkill> _logger;

    public string Name => "MemorySkill";
    public string Description => "Retrieves synthesized high-level case memory, past facts, decisions, and conversation history.";
    public string InputSchemaJson => @"{
        ""type"": ""object"",
        ""properties"": {
            ""aspect"": { ""type"": ""string"", ""description"": ""Optional aspect to focus on: 'summary', 'facts', 'conversations', or 'drafts'"" }
        }
    }";

    public MemorySkill(IMemoryStore memoryStore, ILogger<MemorySkill> logger)
    {
        _memoryStore = memoryStore;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string jsonArguments, McpSession session, CancellationToken cancellationToken = default)
    {
        try
        {
            if (session.CaseId == Guid.Empty)
                return JsonSerializer.Serialize(new { error = "No active case bound to session" });

            _logger.LogInformation("[MemorySkill] Fetching case memory for CaseId {CaseId}", session.CaseId);

            var caseMemory = await _memoryStore.GetCaseMemoryAsync(session.CaseId, cancellationToken);
            var conversations = await _memoryStore.GetRecentConversationsAsync(session.CaseId, 5, cancellationToken);

            return JsonSerializer.Serialize(new
            {
                status = "success",
                caseMemory = caseMemory,
                recentConversations = conversations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MemorySkill] Failed to execute memory skill");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
