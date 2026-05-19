namespace SummaryService.Infrastructure.Llm.Options;

public sealed class AiOptions
{
    public string DefaultProvider { get; set; } = string.Empty;

    public string DefaultModel { get; set; } = string.Empty;

    public Dictionary<string, AiProviderOptions> Providers { get; set; } = [];
}

public sealed class AiProviderOptions
{
    /// <summary>
    /// OpenAI, OpenAICompatible, Ollama, etc.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public List<string> Models { get; set; } = [];
}