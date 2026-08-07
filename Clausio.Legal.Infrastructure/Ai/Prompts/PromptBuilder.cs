using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Clausio.Legal.Core.Interfaces.AI;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Infrastructure.Ai.Prompts;

public class PromptTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string Purpose { get; set; } = string.Empty;
    public List<string> SupportedModels { get; set; } = new();
    public string SystemInstruction { get; set; } = string.Empty;
}

public class PromptBuilder : IPromptBuilder
{
    private readonly string _templatesPath;
    private static readonly ConcurrentDictionary<string, PromptTemplate> _cache = new();
    private readonly ILogger<PromptBuilder>? _logger;

    public PromptBuilder(ILogger<PromptBuilder>? logger = null)
    {
        _logger = logger;
        _templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Clausio.Legal.Infrastructure", "Ai", "Prompts", "Templates");
    }

    public string BuildSystemPrompt(string templateName, Dictionary<string, string>? variables = null)
    {
        var template = LoadTemplate(templateName);
        var prompt = template.SystemInstruction;

        // Remove any un-substituted template variables ({{VAR}} not in the dict)
        if (variables != null)
        {
            foreach (var kvp in variables)
            {
                prompt = prompt.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }
        }

        // Strip any remaining {{PLACEHOLDER}} that weren't filled (avoid leaking template syntax to LLM)
        prompt = System.Text.RegularExpressions.Regex.Replace(prompt, @"\{\{[A-Z_]+\}\}", string.Empty);

        _logger?.LogDebug("[PromptBuilder] Built system prompt from template: {Template} v{Version} (~{Tokens} est. chars)", 
            template.Name, template.Version, prompt.Length);

        return prompt;
    }

    public string BuildUserPrompt(string templateName, string userRequest = "", Dictionary<string, string>? variables = null)
    {
        var prompt = userRequest;

        if (variables != null)
        {
            foreach (var kvp in variables)
            {
                prompt = prompt.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }
        }

        return prompt;
    }

    public string GetTemplateVersion(string templateName)
    {
        var template = LoadTemplate(templateName);
        return template.Version;
    }

    private PromptTemplate LoadTemplate(string templateName)
    {
        // Cache key includes template name for fast repeated access
        if (_cache.TryGetValue(templateName, out var cached))
            return cached;

        var filePath = Path.Combine(_templatesPath, $"{templateName}_v1.json");

        if (!File.Exists(filePath))
        {
            // Development fallback — search relative to working directory
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Clausio.Legal.Infrastructure", "Ai", "Prompts", "Templates", $"{templateName}_v1.json");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Prompt template '{templateName}_v1.json' not found. Searched: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        var template = JsonSerializer.Deserialize<PromptTemplate>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Failed to deserialize prompt template '{templateName}'.");

        _cache.TryAdd(templateName, template);
        _logger?.LogInformation("[PromptBuilder] Loaded template: {Name} v{Version}", template.Name, template.Version);

        return template;
    }
}
