using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.AI.Security;

public class SecurityAssessmentResult
{
    public bool IsBlocked { get; set; }
    public string SanitizedInput { get; set; } = string.Empty;
    public string? FlagReason { get; set; }
}

public interface IAISecurityLayer
{
    Task<SecurityAssessmentResult> AssessAndSanitizeAsync(string userInput, CancellationToken cancellationToken = default);
}
