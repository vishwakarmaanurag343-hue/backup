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
        
        _logger.LogInformation("[Router] PromptType={PromptType}, EstPromptTokens~{Tokens}", promptType, estimatedPromptTokens);
        
        // Build unified pool of free OpenRouter models to try in sequence
        var modelsToTry = new List<string>();
        if (!string.IsNullOrEmpty(_deepModel)) modelsToTry.Add(_deepModel.Trim());
        foreach (var m in _fastModels)
        {
            var trimmed = m.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !modelsToTry.Contains(trimmed))
            {
                modelsToTry.Add(trimmed);
            }
        }

        string result = string.Empty;
        foreach (var model in modelsToTry)
        {
            try
            {
                _logger.LogInformation("[Router] Attempting LLM call with free model: {Model}", model);
                result = await _fastProvider.CompleteAsync(model, systemPrompt, userPrompt, cancellationToken);
                if (!string.IsNullOrEmpty(result))
                {
                    _logger.LogInformation("[Router] Successfully generated completion using model: {Model}", model);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Router] Model {Model} failed ({Error}). Trying next free model in chain...", model, ex.Message);
            }
        }

        if (string.IsNullOrEmpty(result))
        {
            _logger.LogError("[Router] All external free OpenRouter LLM models failed.");
            throw new InvalidOperationException("All AI LLM models failed to generate a response. Please verify your OpenRouter API key or model network access.");
        }

        sw.Stop();
        var estimatedCompletionTokens = result.Length / 4;
        _logger.LogInformation("[Router] Completed. LatencyMs={Ms}, EstCompletionTokens~{Tokens}, TotalTokens~{Total}",
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
        // Deep tasks: require high reasoning, long-form output, or complex drafting
        if (promptType.Equals("LegalDraft", StringComparison.OrdinalIgnoreCase) ||
            promptType.Equals("Summarization", StringComparison.OrdinalIgnoreCase) ||
            promptType.Equals("ActionPlan", StringComparison.OrdinalIgnoreCase) ||
            promptType.Equals("Analysis", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("[Router] Task type '{PromptType}' classified as DEEP.", promptType);
            return "DEEP";
        }

        var combinedLength = systemPrompt.Length + userPrompt.Length;
        if (combinedLength > 12000)
        {
            _logger.LogDebug("[Router] Large prompt ({Chars} chars) classified as DEEP.", combinedLength);
            return "DEEP";
        }

        return "FAST";
    }
}
