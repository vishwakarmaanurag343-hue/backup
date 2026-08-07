namespace Clausio.MCP.Registry;

public class ModelCapability
{
    public required string ModelId { get; set; }
    public bool ToolCalling { get; set; } = true;
    public bool Streaming { get; set; } = true;
    public bool JsonMode { get; set; } = true;
    public bool Vision { get; set; } = false;
    public string Reasoning { get; set; } = "medium";
}
