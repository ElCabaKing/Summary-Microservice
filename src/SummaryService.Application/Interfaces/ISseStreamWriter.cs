namespace SummaryService.Application.Interfaces;

public interface ISseStreamWriter
{
    Task WriteStatusAsync(string status, CancellationToken ct);
    Task WriteChunkAsync(string chunk, CancellationToken ct);
    Task WriteCompletedAsync(string summary, CancellationToken ct);
    Task WriteErrorAsync(string error, CancellationToken ct);
}
