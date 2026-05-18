namespace SummaryService.Application.Interfaces;

public interface ITextChunker
{
    IReadOnlyList<string> Chunk(string text, CancellationToken ct);
}
