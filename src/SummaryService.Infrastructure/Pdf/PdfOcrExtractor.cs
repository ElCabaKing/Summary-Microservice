using System.Diagnostics;
using SummaryService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace SummaryService.Infrastructure.Pdf;

public sealed class PdfOcrExtractor(ILogger<PdfOcrExtractor> logger) : IPdfOcrExtractor
{
    public async Task<string> ExtractTextWithOcrAsync(Stream pdfStream, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Starting OCR extraction via command-line Tesseract");

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var pdfPath = Path.Combine(tempDir, "document.pdf");
            await using (var fs = new FileStream(pdfPath, FileMode.Create, FileAccess.Write))
            {
                await pdfStream.CopyToAsync(fs, ct);
            }

            var text = new System.Text.StringBuilder();

            // Use pdftoppm to convert PDF pages to images
            var ppmProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "pdftoppm",
                    Arguments = $"-png -r 300 \"{pdfPath}\" \"{Path.Combine(tempDir, "page")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                ppmProcess.Start();
                await ppmProcess.WaitForExitAsync(ct);

                if (ppmProcess.ExitCode != 0)
                {
                    var error = await ppmProcess.StandardError.ReadToEndAsync(ct);
                    logger.LogWarning("pdftoppm failed: {Error}. Trying direct Tesseract on PDF.", error);

                    // Fallback: try tesseract directly on PDF
                    return await OcrPdfDirectly(pdfPath, tempDir, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "pdftoppm not available, trying direct Tesseract on PDF");
                return await OcrPdfDirectly(pdfPath, tempDir, ct);
            }

            // OCR each page image
            var pageFiles = Directory.GetFiles(tempDir, "page-*.png")
                .OrderBy(f => f)
                .ToList();

            foreach (var pageFile in pageFiles)
            {
                ct.ThrowIfCancellationRequested();
                var pageText = await OcrImageAsync(pageFile, ct);
                text.AppendLine(pageText);
            }

            logger.LogInformation("OCR completed, extracted {Length} characters", text.Length);
            return text.ToString();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    private static async Task<string> OcrPdfDirectly(string pdfPath, string tempDir, CancellationToken ct)
    {
        var text = new System.Text.StringBuilder();
        var tesseractProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"\"{pdfPath}\" stdout -l eng",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        tesseractProcess.Start();
        var output = await tesseractProcess.StandardOutput.ReadToEndAsync(ct);
        await tesseractProcess.WaitForExitAsync(ct);

        if (tesseractProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
        {
            text.Append(output);
        }

        return text.ToString();
    }

    private static async Task<string> OcrImageAsync(string imagePath, CancellationToken ct)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"\"{imagePath}\" stdout -l eng",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return output ?? string.Empty;
    }
}
