using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SummaryService.Domain.Options;
using SummaryService.Infrastructure.Chunking;

namespace SummaryService.UnitTests.Infrastructure;

public class TextChunkerTests
{
    private readonly TextChunker _chunker;

    public TextChunkerTests()
    {
        var options = Options.Create(new ChunkingOptions
        {
            MaxChunkTokens = 100,
            OverlapTokens = 10
        });
        var logger = NullLogger<TextChunker>.Instance;
        _chunker = new TextChunker(options, logger);
    }

    [Fact]
    public void Chunk_NullText_ReturnsEmpty()
    {
        var result = _chunker.Chunk(null!, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_EmptyText_ReturnsEmpty()
    {
        var result = _chunker.Chunk("", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_ShortText_ReturnsSingleChunk()
    {
        var text = "Hello world. This is a short text.";

        var result = _chunker.Chunk(text, CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public void Chunk_LongText_SplitsIntoMultipleChunks()
    {
        var text = string.Join("\n\n", Enumerable.Range(0, 20).Select(i =>
            new string('a', 200)));

        var result = _chunker.Chunk(text, CancellationToken.None);

        Assert.True(result.Count > 1, "Long text should be split into multiple chunks");
    }

    [Fact]
    public void Chunk_AllChunks_AreWithinTokenLimit()
    {
        var words = string.Join(" ", Enumerable.Repeat("word", 200));
        var text = string.Join("\n\n", Enumerable.Range(0, 5).Select(i => words));

        var result = _chunker.Chunk(text, CancellationToken.None);

        foreach (var chunk in result)
        {
            var estimatedTokens = chunk.Length / 4 + 1;
            Assert.True(estimatedTokens <= 150,
                $"Chunk has ~{estimatedTokens} tokens, exceeding limit");
        }
    }

    [Fact]
    public void Chunk_NoChunk_IsEmptyOrWhitespace()
    {
        var text = "Single paragraph with enough content to be meaningful.";

        var result = _chunker.Chunk(text, CancellationToken.None);

        foreach (var chunk in result)
        {
            Assert.False(string.IsNullOrWhiteSpace(chunk));
        }
    }
}
