using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.AI.Handlers;

public interface IAnalysisHandler
{
    Task<string> SummarizeCaseAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> GenerateChronologyAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> DetectContradictionsAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> AnalyzeEvidenceAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<string> ResearchAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> GenerateActionPlanAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> AnalyzeFinancialsAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> AssessReadinessAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> PrepHearingAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<string> PrepWitnessAsync(Guid caseId, CancellationToken cancellationToken = default);
}
