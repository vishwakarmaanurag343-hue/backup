using System;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities.AI;
using Clausio.Legal.Core.Interfaces.AI.Evaluation;
using Clausio.Legal.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.AI.Evaluation;

public class TelemetryService : ITelemetryService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(IServiceScopeFactory scopeFactory, ILogger<TelemetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogInteractionAsync(AiTelemetryLog log, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ClausioDbContext>();

            dbContext.AiTelemetryLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[TelemetryService] Logged AI interaction for CaseId {CaseId}. TotalTokens={TotalTokens}, Latency={LatencyMs}ms", 
                log.CaseId, log.TotalTokens, log.LatencyMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TelemetryService] Failed to log AI interaction for CaseId {CaseId}", log.CaseId);
        }
    }
}
