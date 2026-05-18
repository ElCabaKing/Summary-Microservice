using SummaryService.Application.Interfaces;
using SummaryService.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace SummaryService.Infrastructure.Chunking;

public sealed class TextChunker(ITokenEstimator tokenEstimator, ILogger<TextChunker> logger) : ITextChunker
{
    public IReadOnlyList<string> Chunk(string text, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var currentChunk = new System.Text.StringBuilder();
        var maxTokens = AppConstants.MaxChunkTokens;
        var overlapTokens = AppConstants.OverlapTokens;

        foreach (var paragraph in paragraphs)
        {
            ct.ThrowIfCancellationRequested();

            var paragraphTokens = tokenEstimator.EstimateTokens(paragraph.ToString());

            if (tokenEstimator.EstimateTokens(currentChunk.ToString()) + paragraphTokens > maxTokens
                && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();

                // Add overlap from previous chunk
                if (chunks.Count > 0 && overlapTokens > 0)
                {
                    var lastChunk = chunks[^1];
                    var words = lastChunk.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var overlapWordCount = Math.Min(words.Length, overlapTokens / 2);
                    if (overlapWordCount > 0)
                    {
                        currentChunk.Append(string.Join(' ', words[^overlapWordCount..]));
                        currentChunk.AppendLine();
                    }
                }
            }

            currentChunk.AppendLine(paragraph);
            currentChunk.AppendLine();
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        logger.LogInformation("Chunked document into {ChunkCount} chunks", chunks.Count);
        return chunks;
    }
}
