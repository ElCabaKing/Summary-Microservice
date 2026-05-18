namespace SummaryService.Application.Interfaces;

public interface IPdfOcrExtractor
{
    Task<string> ExtractTextWithOcrAsync(Stream pdfStream, CancellationToken ct);
}
