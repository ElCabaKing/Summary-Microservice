namespace SummaryService.Domain.Options;

public sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";

    public int MaxChunkTokens { get; init; } = 500;

    public int OverlapTokens { get; init; } = 25;

    public int MaxChunks { get; init; } = 100;
}
