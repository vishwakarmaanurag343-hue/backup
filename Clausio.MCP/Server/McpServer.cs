using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clausio.MCP.Interfaces;
using Clausio.MCP.Session;
using Microsoft.Extensions.Logging;

namespace Clausio.MCP.Server;

public class McpServer
{
    private readonly IEnumerable<IMcpSkill> _skills;
    private readonly ILogger<McpServer> _logger;

    public McpServer(IEnumerable<IMcpSkill> skills, ILogger<McpServer> logger)
    {
        _skills = skills;
        _logger = logger;
    }

    public async Task<string> ExecuteSkillAsync(string skillName, string jsonArguments, McpSession session, CancellationToken cancellationToken = default)
    {
        var skill = _skills.FirstOrDefault(s => string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));
        if (skill == null)
        {
            _logger.LogWarning("[McpServer] Skill '{SkillName}' not found in registry", skillName);
            return $"{{\"error\": \"Skill '{skillName}' is not registered\"}}";
        }

        // Cache check
        var cacheKey = $"{skillName}_{jsonArguments.GetHashCode()}";
        if (session.ToolCache.TryGetValue(cacheKey, out var cachedResponse))
        {
            _logger.LogInformation("[McpServer] Cache hit for skill '{SkillName}'", skillName);
            return cachedResponse;
        }

        _logger.LogInformation("[McpServer] Audit Log [Start]: Executing skill '{SkillName}' for Session {SessionId}, Case {CaseId}", skillName, session.SessionId, session.CaseId);

        var startTime = DateTime.UtcNow;
        var result = await skill.ExecuteAsync(jsonArguments, session, cancellationToken);
        var durationMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

        _logger.LogInformation("[McpServer] Audit Log [End]: Skill '{SkillName}' completed in {DurationMs}ms", skillName, durationMs);

        // Update session cache
        session.ToolCache[cacheKey] = result;

        return result;
    }
}
