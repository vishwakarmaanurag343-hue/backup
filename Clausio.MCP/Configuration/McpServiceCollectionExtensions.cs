using Clausio.MCP.Interfaces;
using Clausio.MCP.Planners;
using Clausio.MCP.Registry;
using Clausio.MCP.Server;
using Clausio.MCP.Skills;
using Microsoft.Extensions.DependencyInjection;

namespace Clausio.MCP.Configuration;

public static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddClausioMcp(this IServiceCollection services)
    {
        // Core Registry & Planners
        services.AddSingleton<AiCapabilityRegistry>();
        services.AddSingleton<WorkflowPlanner>();
        services.AddScoped<CapabilityPlanner>();

        // Server
        services.AddScoped<McpServer>();

        // Skills
        services.AddScoped<IMcpSkill, ResearchSkill>();
        services.AddScoped<IMcpSkill, MemorySkill>();

        return services;
    }
}
