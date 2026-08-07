using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.Embedding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Infrastructure.Ai.Providers;

public class OpenAIEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAIEmbeddingProvider> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly string _modelId;

    public OpenAIEmbeddingProvider(IConfiguration config, ILogger<OpenAIEmbeddingProvider> logger, HttpClient httpClient)
    {
        _logger = logger;
        _http = httpClient;
        _apiKey = config["AI:EmbeddingProvider:ApiKey"] ?? throw new InvalidOperationException("AI:EmbeddingProvider:ApiKey missing");
        _baseUrl = config["AI:EmbeddingProvider:BaseUrl"] ?? "https://api.openai.com/v1";
        _modelId = config["AI:EmbeddingProvider:ModelId"] ?? "text-embedding-3-small";
        
        _http.DefaultRequestHeaders.Add("User-Agent", "ClausioLegalAI/1.0");
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embeddings = await GenerateEmbeddingsAsync(new List<string> { text }, cancellationToken);
        return embeddings.FirstOrDefault() ?? Array.Empty<float>();
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating embeddings for {Count} texts using model {Model}", texts.Count, _modelId);

        var requestBody = new
        {
            model = _modelId,
            input = texts
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = content;

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonDocument.Parse(responseJson);

        var result = new List<float[]>();
        var dataArray = parsed.RootElement.GetProperty("data");
        
        foreach (var dataItem in dataArray.EnumerateArray())
        {
            var embeddingArray = dataItem.GetProperty("embedding");
            var floats = new float[embeddingArray.GetArrayLength()];
            int i = 0;
            foreach (var num in embeddingArray.EnumerateArray())
            {
                floats[i++] = num.GetSingle();
            }
            result.Add(floats);
        }

        return result;
    }
}
