using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clausio.Legal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/ai-analytics")]
public class AiAnalyticsController : ControllerBase
{
    private readonly ClausioDbContext _dbContext;

    public AiAnalyticsController(ClausioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var logs = await _dbContext.AiTelemetryLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(1000)
            .ToListAsync(cancellationToken);

        if (!logs.Any())
        {
            return Ok(new
            {
                TotalRequests = 0,
                AverageLatencyMs = 0,
                AverageTokens = 0,
                SuccessRate = 0
            });
        }

        return Ok(new
        {
            TotalRequests = await _dbContext.AiTelemetryLogs.CountAsync(cancellationToken),
            AverageLatencyMs = logs.Average(l => l.LatencyMs),
            AverageTokens = logs.Average(l => l.TotalTokens),
            SuccessRate = logs.Count(l => l.IsSuccess) * 100.0 / logs.Count,
            TotalTokens30Days = logs.Sum(l => l.TotalTokens)
        });
    }

    [HttpGet("quality")]
    public async Task<IActionResult> GetQualityMetrics(CancellationToken cancellationToken)
    {
        var logs = await _dbContext.AiTelemetryLogs
            .Where(l => l.IsSuccess)
            .OrderByDescending(l => l.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        if (!logs.Any()) return Ok(new { });

        return Ok(new
        {
            AverageRetrievalScore = logs.Average(l => l.RetrievalScore),
            AverageDraftScore = logs.Average(l => l.DraftScore),
            AverageCitationConfidence = logs.Average(l => l.CitationConfidenceScore),
            AverageHallucinationRisk = logs.Average(l => l.HallucinationRiskScore)
        });
    }

    [HttpGet("models")]
    public async Task<IActionResult> GetModelUsage(CancellationToken cancellationToken)
    {
        var logs = await _dbContext.AiTelemetryLogs
            .GroupBy(l => l.Model)
            .Select(g => new { Model = g.Key, Count = g.Count(), AverageLatency = g.Average(x => x.LatencyMs) })
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }
}
