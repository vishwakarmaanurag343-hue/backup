using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Infrastructure.Ai.Providers;

public class TokenRouterProvider : ILLMProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<TokenRouterProvider> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public TokenRouterProvider(IConfiguration config, ILogger<TokenRouterProvider> logger, HttpClient httpClient)
    {
        _logger = logger;
        _http = httpClient;
        _apiKey = config["AI:DeepProvider:ApiKey"] ?? throw new InvalidOperationException("AI:DeepProvider:ApiKey missing");
        _baseUrl = config["AI:DeepProvider:BaseUrl"] ?? "https://openrouter.ai/api/v1";
        
        _http.DefaultRequestHeaders.Add("User-Agent", "ClausioLegalAI/1.0");
        _http.Timeout = TimeSpan.FromSeconds(180);
    }

    public async Task<string> CompleteAsync(string model, string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TokenRouter CompleteAsync called for model {Model}", model);
        return await CallApiAsync(model, systemPrompt, userPrompt, false, cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(string model, string systemPrompt, string userPrompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TokenRouter StreamCompleteAsync called for model {Model}", model);

        var requestBody = new
        {
            model = model,
            max_tokens = 2048,
            temperature = 0.1,
            stream = true,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = content;

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") break;

                string chunk = "";
                try 
                {
                    var parsed = JsonDocument.Parse(data);
                    var delta = parsed.RootElement.GetProperty("choices")[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentProp))
                    {
                        chunk = contentProp.GetString();
                    }
                }
                catch { /* Ignore parse errors for partial chunks */ }

                if (!string.IsNullOrEmpty(chunk))
                {
                    yield return chunk;
                }
            }
        }
    }

    private async Task<string> CallApiAsync(string model, string systemPrompt, string userPrompt, bool stream, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = model,
            max_tokens = 2048,
            temperature = 0.1,
            stream = stream,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = content;

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("[TokenRouterProvider] Model {Model} returned {Status}: {Error}", model, response.StatusCode, errBody);
            throw new HttpRequestException($"Model {model} returned {response.StatusCode}: {errBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonDocument.Parse(responseJson);

        var responseText = parsed.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return ExtractJson(responseText);
    }

    private string ExtractJson(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"```json\s*(\{.*?\})\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (match.Success) return match.Groups[1].Value;

        var startIndex = text.IndexOf('{');
        var endIndex = text.LastIndexOf('}');
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return text.Substring(startIndex, endIndex - startIndex + 1);
        }
        return text;
    }
}
