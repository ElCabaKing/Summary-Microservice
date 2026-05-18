namespace SummaryService.Domain.Enums;

public enum SummaryStatus
{
    ExtractingText,
    RunningOcr,
    ChunkingDocument,
    GeneratingSummary,
    ReducingSummary,
    Completed,
    Failed
}
