
using SummaryService.Application.Models;

namespace SummaryService.Application.Interfaces;
public interface IStreamingTextGenerator
{
    IAsyncEnumerable<string> GenerateStreamAsync(
        AiRequest request,
        CancellationToken ct = default);
}