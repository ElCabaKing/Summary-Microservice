namespace SummaryService.Application.Models;
public sealed class AiRequest
{
    public string Provider { get; init; } = default!;

    public string Model { get; init; } = default!;

    public string Prompt { get; init; } = default!;

    public float Temperature { get; init; } = 0.2f;

    public int? MaxTokens { get; init; }
}
