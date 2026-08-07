using System.Threading;
using System.Threading.Tasks;
using Clausio.MCP.Session;

namespace Clausio.MCP.Interfaces;

public interface IMcpSkill
{
    string Name { get; }
    string Description { get; }
    string InputSchemaJson { get; }
    Task<string> ExecuteAsync(string jsonArguments, McpSession session, CancellationToken cancellationToken = default);
}
