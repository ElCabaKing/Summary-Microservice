namespace SummaryService.Application.Interfaces;

public interface IDocumentParser
{
    Task<string> ParseAsync(Stream document, string contentType, CancellationToken ct);
}
