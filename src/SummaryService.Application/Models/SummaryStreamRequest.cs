namespace SummaryService.Application.Models;

public sealed class SummaryStreamRequest
{
    public required Stream Document { get; init; }

    public required string ContentType { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public float Temperature { get; init; } = 0.2f;

    public int? MaxTokens { get; init; }
}