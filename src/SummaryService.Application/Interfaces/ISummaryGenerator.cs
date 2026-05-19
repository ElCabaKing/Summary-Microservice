using SummaryService.Application.Models;

namespace SummaryService.Application.Interfaces;

public interface ISummaryGenerator
{
    IAsyncEnumerable<string> SummarizeAsync(AiRequest aiRequest, CancellationToken ct);
}
