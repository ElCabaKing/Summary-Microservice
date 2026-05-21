namespace SummaryService.Domain.Options;

public sealed class SummaryOptions
{
    public const string SectionName = "Summary";

    public int MaxFileSizeMb { get; init; } = 15;

    public int MaxTokens { get; init; } = 2048;
}
