using SummaryService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace SummaryService.Infrastructure.Pdf;

public sealed class DocumentParser(
    IPdfTextExtractor pdfTextExtractor,
    IPdfOcrExtractor pdfOcrExtractor,
    ILogger<DocumentParser> logger) : IDocumentParser
{
    public async Task<string> ParseAsync(Stream document, string contentType, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (contentType == "text/plain")
        {
            using var reader = new StreamReader(document, leaveOpen: true);
            return await reader.ReadToEndAsync(ct);
        }

        if (contentType != "application/pdf")
            throw new NotSupportedException($"Content type {contentType} is not supported");

        logger.LogInformation("Parsing PDF document");

        var text = await pdfTextExtractor.ExtractTextAsync(document, ct);

        if (string.IsNullOrWhiteSpace(text) || text.Length < 50)
        {
            logger.LogInformation("Native PDF text insufficient ({Length} chars), falling back to OCR", text?.Length ?? 0);
            document.Position = 0;
            text = await pdfOcrExtractor.ExtractTextWithOcrAsync(document, ct);
        }

        return NormalizeText(text);
    }

    private static string NormalizeText(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();
    }
}
