using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.AI;
using Clausio.Legal.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Clausio.Legal.Infrastructure.Ai.Router;

public class AIRouter : IAIRouter
{
    private readonly TokenRouterProvider _deepProvider;
    private readonly OpenRouterProvider _fastProvider;
    private readonly ILogger<AIRouter> _logger;
    private readonly string _deepModel;
    private readonly string[] _fastModels;
    private readonly AsyncRetryPolicy _fastRetryPolicy;

    public AIRouter(
        TokenRouterProvider deepProvider, 
        OpenRouterProvider fastProvider, 
        IConfiguration config, 
        ILogger<AIRouter> logger)
    {
        _deepProvider = deepProvider;
        _fastProvider = fastProvider;
        _logger = logger;

        _deepModel = config["AI:DeepProvider:ModelId"] ?? "google/gemma-2-9b-it:free";
        var modelsSection = config.GetSection("AI:FastProvider:FallbackModels")
            .GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .Select(v => v!)
            .ToArray();

        if (modelsSection.Length > 0)
        {
            _fastModels = modelsSection;
        }
        else
        {
            _fastModels = new[]
            {
                "google/gemma-2-9b-it:free",
                "mistralai/mistral-7b-instruct:free",
                "qwen/qwen-2.5-coder-32b-instruct:free",
                "meta-llama/llama-3.1-8b-instruct:free"
            };
        }

        // Polly retry policy: Retry once for fast models
        _fastRetryPolicy = Policy
            .Handle<Exception>()
            .RetryAsync(1, onRetry: (exception, retryCount) =>
            {
                _logger.LogWarning("Fast provider failed. Retrying... (Attempt {RetryCount}). Error: {Error}", retryCount, exception.Message);
            });
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, string promptType = "chat", CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var estimatedPromptTokens = (systemPrompt.Length + userPrompt.Length) / 4;
        var modelType = SelectModelType(promptType, systemPrompt, userPrompt);
        
        _logger.LogInformation("[Router:Complete] PromptType={PromptType}, ModelType={ModelType}, EstPromptTokens~{Tokens}", promptType, modelType, estimatedPromptTokens);
        
        // Build ordered list of models based on complexity classification
        var modelsToTry = new List<string>();
        if (modelType == "DEEP")
        {
            if (!string.IsNullOrEmpty(_deepModel)) modelsToTry.Add(_deepModel.Trim());
            foreach (var m in _fastModels)
            {
                var trimmed = m.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !modelsToTry.Contains(trimmed)) modelsToTry.Add(trimmed);
            }
        }
        else
        {
            foreach (var m in _fastModels)
            {
                var trimmed = m.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !modelsToTry.Contains(trimmed)) modelsToTry.Add(trimmed);
            }
            if (!string.IsNullOrEmpty(_deepModel) && !modelsToTry.Contains(_deepModel.Trim())) modelsToTry.Add(_deepModel.Trim());
        }

        string result = string.Empty;
        foreach (var model in modelsToTry)
        {
            try
            {
                _logger.LogInformation("[Router:Complete] Attempting LLM call with model: {Model} (Target: {Type})", model, modelType);
                result = await _fastProvider.CompleteAsync(model, systemPrompt, userPrompt, cancellationToken);
                if (!string.IsNullOrEmpty(result))
                {
                    _logger.LogInformation("[Router:Complete] Successfully generated completion using model: {Model}", model);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Router:Complete] Model {Model} failed ({Error}). Trying next fallback model...", model, ex.Message);
            }
        }

        if (string.IsNullOrEmpty(result))
        {
            _logger.LogError("[Router:Complete] All external LLM models failed.");
            throw new InvalidOperationException("All AI LLM models failed to generate a response. Please verify your API key or model network access.");
        }

        sw.Stop();
        var estimatedCompletionTokens = result.Length / 4;
        _logger.LogInformation("[Router:Complete] Completed. LatencyMs={Ms}, EstCompletionTokens~{Tokens}, TotalTokens~{Total}",
            sw.ElapsedMilliseconds, estimatedCompletionTokens, estimatedPromptTokens + estimatedCompletionTokens);
        
        return result;
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(string systemPrompt, string userPrompt, string promptType = "chat", [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var modelType = SelectModelType(promptType, systemPrompt, userPrompt);
        
        var modelsToTry = new List<string>();
        if (modelType == "DEEP")
        {
            modelsToTry.Add(_deepModel);
        }
        else
        {
            modelsToTry.AddRange(_fastModels.Select(m => m.Trim()));
            if (!modelsToTry.Contains(_deepModel))
            {
                modelsToTry.Add(_deepModel);
            }
        }

        foreach (var model in modelsToTry)
        {
            _logger.LogInformation("[Router] Attempting to stream with model: {Model}", model);
            bool hasYielded = false;
            bool failed = false;

            IAsyncEnumerable<string> stream;
            try
            {
                stream = _fastProvider.StreamCompleteAsync(model, systemPrompt, userPrompt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Router] Failed to initialize stream for model {Model}: {Error}. Trying next fallback...", model, ex.Message);
                continue;
            }

            var enumerator = stream.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (true)
                {
                    bool moveNext;
                    try
                    {
                        moveNext = await enumerator.MoveNextAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("[Router] Streaming model {Model} failed: {Error}.", model, ex.Message);
                        failed = true;
                        break;
                    }

                    if (!moveNext) break;

                    hasYielded = true;
                    yield return enumerator.Current;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            if (!failed && hasYielded)
            {
                // Completed successfully!
                yield break;
            }

            if (hasYielded)
            {
                // If it already started streaming chunks to the user before failing mid-stream, stop.
                yield break;
            }

            _logger.LogWarning("[Router] Model {Model} failed before producing output. Automatically switching to next model...", model);
        }
    }

    private string SelectModelType(string promptType, string systemPrompt, string userPrompt)
    {
        int complexityScore = CalculateComplexityScore(promptType, systemPrompt, userPrompt);
        
        if (complexityScore >= 60)
        {
            _logger.LogInformation("[Router] Task '{PromptType}' assigned DEEP model (ComplexityScore={Score}).", promptType, complexityScore);
            return "DEEP";
        }

        _logger.LogInformation("[Router] Task '{PromptType}' assigned FAST model (ComplexityScore={Score}).", promptType, complexityScore);
        return "FAST";
    }

    private int CalculateComplexityScore(string promptType, string systemPrompt, string userPrompt)
    {
        int score = 0;

        // 1. Task Type Base Rules
        score += promptType.ToLowerInvariant() switch
        {
            "legaldraft" => 50,
            "deepresearch" => 60,
            "analysis" => 40,
            "contradiction" => 45,
            "actionplan" => 35,
            "summarization" => 30,
            "prep" => 25,
            _ => 10
        };

        // 2. Length-based Heuristics
        var combinedLen = systemPrompt.Length + userPrompt.Length;
        if (combinedLen > 10000) score += 35;
        else if (combinedLen > 4000) score += 20;

        // 3. Legal Statutory Keyword Density Rules
        var legalKeywords = new[] { "supreme court", "high court", "section", "article", "ipc", "crpc", "cpc", "statute", "precedent", "ratio decidendi", "interim relief", "stay order", "affidavit", "writ petition" };
        var lowerPrompt = userPrompt.ToLowerInvariant();
        foreach (var kw in legalKeywords)
        {
            if (lowerPrompt.Contains(kw)) score += 5;
        }

        return score;
    }
}
