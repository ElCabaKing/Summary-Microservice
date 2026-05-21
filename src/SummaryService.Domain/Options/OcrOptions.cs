namespace SummaryService.Domain.Options;

public sealed class OcrOptions
{
    public const string SectionName = "OCR";

    public int MaxPages { get; init; } = 100;

    public int Dpi { get; init; } = 300;

    public int TimeoutSeconds { get; init; } = 60;
}
