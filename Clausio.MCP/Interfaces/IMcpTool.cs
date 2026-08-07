using System.Threading;
using System.Threading.Tasks;
using Clausio.MCP.Session;

namespace Clausio.MCP.Interfaces;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    string InputSchemaJson { get; }
    int AverageLatencyMs { get; }
    bool IsCacheable { get; }
    Task<string> ExecuteAsync(string jsonArguments, McpSession session, CancellationToken cancellationToken = default);
}
