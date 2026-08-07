using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Clausio.MCP.Interfaces;
using Clausio.MCP.Session;
using Microsoft.Extensions.Logging;

namespace Clausio.MCP.Skills;

public class ResearchSkill : IMcpSkill
{
    private readonly IRetrievalEngine _retrievalEngine;
    private readonly ILogger<ResearchSkill> _logger;

    public string Name => "ResearchSkill";
    public string Description => "Performs deep hybrid legal research across case documents, evidence, and precedent clauses.";
    public string InputSchemaJson => @"{
        ""type"": ""object"",
        ""properties"": {
            ""query"": { ""type"": ""string"", ""description"": ""The specific legal query or keyword to search for"" }
        },
        ""required"": [""query""]
    }";

    public ResearchSkill(IRetrievalEngine retrievalEngine, ILogger<ResearchSkill> logger)
    {
        _retrievalEngine = retrievalEngine;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string jsonArguments, McpSession session, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonArguments);
            var query = doc.RootElement.GetProperty("query").GetString();

            if (string.IsNullOrWhiteSpace(query))
                return JsonSerializer.Serialize(new { error = "Query cannot be empty" });

            // Session isolation check
            if (session.CaseId == Guid.Empty)
                return JsonSerializer.Serialize(new { error = "No active case bound to session" });

            _logger.LogInformation("[ResearchSkill] Executing hybrid retrieval for query '{Query}' on CaseId {CaseId}", query, session.CaseId);

            var chunks = await _retrievalEngine.GetContextAsync(query, session.CaseId, cancellationToken);

            return JsonSerializer.Serialize(new
            {
                status = "success",
                resultCount = chunks.Count,
                results = chunks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResearchSkill] Failed to execute research skill");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
