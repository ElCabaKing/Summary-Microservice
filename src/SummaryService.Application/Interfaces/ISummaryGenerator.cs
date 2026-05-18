namespace SummaryService.Application.Interfaces;

public interface ISummaryGenerator
{
    IAsyncEnumerable<string> SummarizeAsync(string text, CancellationToken ct);
}
