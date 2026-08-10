using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.AI.Security;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.AI.Security;

public class AISecurityLayer : IAISecurityLayer
{
    private readonly ILogger<AISecurityLayer> _logger;

    // Common jailbreak and injection strings
    private static readonly string[] JailbreakKeywords =
    {
        "ignore previous instructions",
        "ignore all previous instructions",
        "developer mode",
        "system prompt",
        "you are now dan",
        "do anything now",
        "forget everything",
        "bypass rules",
        "act as an uncensored",
        "jailbreak",
        "print your prompt"
    };

    public AISecurityLayer(ILogger<AISecurityLayer> logger)
    {
        _logger = logger;
    }

    public Task<SecurityAssessmentResult> AssessAndSanitizeAsync(string userInput, CancellationToken cancellationToken = default)
    {
        var result = new SecurityAssessmentResult
        {
            IsBlocked = false,
            SanitizedInput = userInput
        };

        if (string.IsNullOrWhiteSpace(userInput))
        {
            return Task.FromResult(result);
        }

        // 1. Sanitize (Phase D)
        // Remove zero-width characters (invisible characters)
        result.SanitizedInput = Regex.Replace(result.SanitizedInput, @"[\u200B-\u200D\uFEFF]", "");
        
        // Normalize whitespace
        result.SanitizedInput = Regex.Replace(result.SanitizedInput, @"\s+", " ").Trim();

        // Strip basic HTML/Scripts if present (simple sanitization for pipelines)
        result.SanitizedInput = Regex.Replace(result.SanitizedInput, @"<script.*?>.*?</script>", "", RegexOptions.IgnoreCase);

        // 2. Assess for Injection/Jailbreaks (Phase B & C)
        var lowerInput = result.SanitizedInput.ToLowerInvariant();
        
        // 2. SQL Injection Rule Detection
        var sqlKeywords = new[] { "drop table", "union select", "; delete from", "; update ", "alter table", "exec(" };
        var matchedSql = sqlKeywords.FirstOrDefault(sql => lowerInput.Contains(sql));
        if (matchedSql != null)
        {
            _logger.LogWarning("[SecurityLayer] SQL Injection attempt detected: {SqlKeyword}", matchedSql);
            result.IsBlocked = true;
            result.FlagReason = $"Malicious SQL pattern detected ({matchedSql})";
            return Task.FromResult(result);
        }

        // 3. Jailbreak/Prompt Injection Rule Detection
        var matchedKeyword = JailbreakKeywords.FirstOrDefault(kw => lowerInput.Contains(kw));
        if (matchedKeyword != null)
        {
            _logger.LogWarning("[SecurityLayer] Jailbreak/Injection attempt detected: {Keyword}", matchedKeyword);
            result.IsBlocked = true;
            result.FlagReason = $"Prompt Injection attempt detected ({matchedKeyword})";
            return Task.FromResult(result);
        }

        // 4. Rule-Based PII Masking (Aadhaar & PAN Numbers)
        result.SanitizedInput = Regex.Replace(result.SanitizedInput, @"\b\d{4}\s?\d{4}\s?\d{4}\b", "[AADHAAR_REDACTED]");
        result.SanitizedInput = Regex.Replace(result.SanitizedInput, @"\b[A-Z]{5}\d{4}[A-Z]{1}\b", "[PAN_REDACTED]");

        return Task.FromResult(result);
    }
}
