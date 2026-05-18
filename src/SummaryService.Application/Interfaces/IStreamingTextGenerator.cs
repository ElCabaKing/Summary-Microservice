namespace SummaryService.Application.Interfaces;

public interface IStreamingTextGenerator
{
    IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken ct);
}
