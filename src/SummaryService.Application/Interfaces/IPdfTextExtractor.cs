namespace SummaryService.Application.Interfaces;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken ct);
}
