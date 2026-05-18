using UglyToad.PdfPig;
using SummaryService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace SummaryService.Infrastructure.Pdf;

public sealed class PdfTextExtractor(ILogger<PdfTextExtractor> logger) : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var pdf = PdfDocument.Open(pdfStream);
        var text = new System.Text.StringBuilder();

        foreach (var page in pdf.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            text.AppendLine(page.Text);
        }

        var result = text.ToString();
        logger.LogInformation("Extracted {Length} characters from PDF", result.Length);

        return Task.FromResult(result);
    }
}
