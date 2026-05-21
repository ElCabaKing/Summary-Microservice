using SummaryService.Application.Interfaces;
using SummaryService.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace SummaryService.Infrastructure.Chunking;

/// <summary>
/// Splits text into chunks constrained by <see cref="AppConstants.MaxChunkTokens"/>.
/// Process overview:
/// 1) Split the document by double line breaks (paragraphs).
/// 2) Split oversized paragraphs into smaller sub-paragraphs (by lines, then by words).
/// 3) Append sub-paragraphs to the current chunk until the token limit is reached.
/// 4) When a chunk is closed, prepend a small overlap from the previous chunk.
/// </summary>
public sealed class TextChunker(ITokenEstimator tokenEstimator, ILogger<TextChunker> logger) : ITextChunker
{
    /// <summary>
    /// Creates chunked text segments from a full document while preserving continuity
    /// with a token overlap between consecutive chunks.
    /// </summary>
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

            // Divide large paragraphs into smaller pieces
            var subParagraphs = DivideParagraphIfNeeded(paragraph, maxTokens, ct);

            foreach (var subParagraph in subParagraphs)
            {
                ct.ThrowIfCancellationRequested();

                var subParaTokens = tokenEstimator.EstimateTokens(subParagraph);
                var currentTokens = tokenEstimator.EstimateTokens(currentChunk.ToString());

                if (currentTokens + subParaTokens > maxTokens && currentChunk.Length > 0)
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

    /// <summary>
    /// Splits a paragraph only when it exceeds token limits.
    /// It tries line-based splitting first, and falls back to word-based splitting.
    /// </summary>
    private List<string> DivideParagraphIfNeeded(string paragraph, int maxTokens, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var paragraphTokens = tokenEstimator.EstimateTokens(paragraph);

        // If paragraph fits within max tokens, return as-is
        if (paragraphTokens <= maxTokens)
            return [paragraph];

        // First, try dividing by single line breaks
        var lines = paragraph.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 1)
        {
            var result = new List<string>();
            var currentLine = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                ct.ThrowIfCancellationRequested();

                var lineTokens = tokenEstimator.EstimateTokens(line);
                var currentTokens = tokenEstimator.EstimateTokens(currentLine.ToString());

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

        // If no line breaks, divide by words
        var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var currentWords = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            ct.ThrowIfCancellationRequested();

            var wordTokens = tokenEstimator.EstimateTokens(word);
            var currentTokens = tokenEstimator.EstimateTokens(currentWords.ToString());

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
