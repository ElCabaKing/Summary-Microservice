using Microsoft.Extensions.Options;
using SummaryService.Application.Interfaces;
using SummaryService.Application.Services;
using SummaryService.Domain.Options;
using Microsoft.Extensions.Logging;

namespace SummaryService.Infrastructure.Chunking;

public sealed class TextChunker(IOptions<ChunkingOptions> chunkingOptions, ILogger<TextChunker> logger) : ITextChunker
{
    public IReadOnlyList<string> Chunk(string text, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var currentChunk = new System.Text.StringBuilder();
        var maxTokens = chunkingOptions.Value.MaxChunkTokens;
        var overlapTokens = chunkingOptions.Value.OverlapTokens;

        foreach (var paragraph in paragraphs)
        {
            ct.ThrowIfCancellationRequested();

            var subParagraphs = DivideParagraphIfNeeded(paragraph, maxTokens, ct);

            foreach (var subParagraph in subParagraphs)
            {
                ct.ThrowIfCancellationRequested();

                var subParaTokens = TokenEstimator.Estimate(subParagraph);
                var currentTokens = TokenEstimator.Estimate(currentChunk.ToString());

                if (currentTokens + subParaTokens > maxTokens && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();

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

                currentChunk.AppendLine(subParagraph);
                currentChunk.AppendLine();
            }
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        logger.LogInformation("Chunked document into {ChunkCount} chunks", chunks.Count);
        return chunks;
    }

    private List<string> DivideParagraphIfNeeded(string paragraph, int maxTokens, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var paragraphTokens = TokenEstimator.Estimate(paragraph);

        if (paragraphTokens <= maxTokens)
            return [paragraph];

        var lines = paragraph.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 1)
        {
            var result = new List<string>();
            var currentLine = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                ct.ThrowIfCancellationRequested();

                var lineTokens = TokenEstimator.Estimate(line);
                var currentTokens = TokenEstimator.Estimate(currentLine.ToString());

                if (currentTokens + lineTokens > maxTokens && currentLine.Length > 0)
                {
                    result.Add(currentLine.ToString().Trim());
                    currentLine.Clear();
                }

                if (currentLine.Length > 0)
                    currentLine.AppendLine();
                currentLine.Append(line);
            }

            if (currentLine.Length > 0)
                result.Add(currentLine.ToString().Trim());

            return result;
        }

        var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var currentWords = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            ct.ThrowIfCancellationRequested();

            var wordTokens = TokenEstimator.Estimate(word);
            var currentTokens = TokenEstimator.Estimate(currentWords.ToString());

            if (currentTokens + wordTokens > maxTokens && currentWords.Length > 0)
            {
                chunks.Add(currentWords.ToString().Trim());
                currentWords.Clear();
            }

            if (currentWords.Length > 0)
                currentWords.Append(' ');
            currentWords.Append(word);
        }

        if (currentWords.Length > 0)
            chunks.Add(currentWords.ToString().Trim());

        return chunks;
    }
}
