using System.Collections.Concurrent;

namespace Clausio.MCP.Registry;

public class AiCapabilityRegistry
{
    private readonly ConcurrentDictionary<string, ModelCapability> _capabilities = new();

    public AiCapabilityRegistry()
    {
        // Seed default capability metadata for supported models
        Register(new ModelCapability { ModelId = "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free", ToolCalling = true, Reasoning = "high" });
        Register(new ModelCapability { ModelId = "google/gemini-2.5-flash", ToolCalling = true, Vision = true, Reasoning = "high" });
        Register(new ModelCapability { ModelId = "moonshotai/kimi-k3", ToolCalling = true, JsonMode = true, Reasoning = "high" });
        Register(new ModelCapability { ModelId = "openai/gpt-4o", ToolCalling = true, Vision = true, Reasoning = "high" });
        Register(new ModelCapability { ModelId = "anthropic/claude-3.5-sonnet", ToolCalling = true, Vision = true, Reasoning = "high" });
        Register(new ModelCapability { ModelId = "meta-llama/llama-3.3-70b-instruct:free", ToolCalling = false, Reasoning = "medium" });
    }

    public void Register(ModelCapability capability)
    {
        _capabilities[capability.ModelId.ToLowerInvariant()] = capability;
    }

    public ModelCapability GetCapability(string modelId)
    {
        var key = modelId.ToLowerInvariant();
        if (_capabilities.TryGetValue(key, out var cap))
        {
            return cap;
        }

        // Default fallback assumption for unknown models
        return new ModelCapability
        {
            ModelId = modelId,
            ToolCalling = true,
            Streaming = true,
            JsonMode = true
        };
    }
}
