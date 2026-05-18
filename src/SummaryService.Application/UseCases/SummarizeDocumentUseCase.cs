using System.Text;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Constants;
using SummaryService.Shared.Models;

namespace SummaryService.Application.UseCases;

public sealed class SummarizeDocumentUseCase(
    IDocumentParser documentParser,
    ITextChunker textChunker,
    IPromptProvider promptProvider,
    ISummaryGenerator summaryGenerator,
    ISseStreamWriter sseWriter)
{
    public async Task<Result> ExecuteAsync(
        Stream document,
        string contentType,
        CancellationToken ct)
    {
        try
        {
            await sseWriter.WriteStatusAsync("extracting_text", ct);

            var text = await documentParser.ParseAsync(
                document,
                contentType,
                ct);

            if (string.IsNullOrWhiteSpace(text))
            {
                await sseWriter.WriteErrorAsync(
                    "No text could be extracted from the document",
                    ct);

                return Result.Failure(
                    new Error(
                        "NO_TEXT",
                        "No text could be extracted"));
            }

            await sseWriter.WriteStatusAsync(
                "chunking_document",
                ct);

            var chunks = textChunker.Chunk(text, ct);

            if (chunks.Count == 0)
            {
                await sseWriter.WriteErrorAsync(
                    "Document is empty after chunking",
                    ct);

                return Result.Failure(
                    new Error(
                        "EMPTY_DOCUMENT",
                        "Document is empty"));
            }

            await sseWriter.WriteStatusAsync(
                "generating_summary",
                ct);

            var chunkSummaries = new List<string>();

            for (var i = 0; i < chunks.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var chunk = chunks[i];

                await sseWriter.WriteStatusAsync(
                    $"summarizing_chunk_{chunk}",
                    ct);

                var chunkPrompt = promptProvider
                    .GetPrompt("summarize-chunk")
                    .Replace("{text}", chunk);

                var chunkSummaryBuilder = new StringBuilder();

                await foreach (var token in summaryGenerator.SummarizeAsync(
                                   chunkPrompt,
                                   ct))
                {
                    chunkSummaryBuilder.Append(token);
                }

                var chunkSummary = chunkSummaryBuilder.ToString().Trim();

                if (!string.IsNullOrWhiteSpace(chunkSummary))
                {
                    chunkSummaries.Add(chunkSummary);
                }
            }

            string finalSummary;

            if (chunkSummaries.Count > 1)
            {
                await sseWriter.WriteStatusAsync(
                    "reducing_summary",
                    ct);

                var combinedSummaries = string.Join(
                    "\n\n",
                    chunkSummaries);

                var reducePrompt = promptProvider
                    .GetPrompt("reduce")
                    .Replace("{text}", combinedSummaries);

                var finalSummaryBuilder = new StringBuilder();

                await foreach (var token in summaryGenerator.SummarizeAsync(
                                   reducePrompt,
                                   ct))
                {
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        continue;
                    }

                    finalSummaryBuilder.Append(token);

                    await sseWriter.WriteChunkAsync(
                        token,
                        ct);
                }

                finalSummary = finalSummaryBuilder
                    .ToString()
                    .Trim();
            }
            else
            {
                finalSummary = chunkSummaries.FirstOrDefault()
                               ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(finalSummary))
                {
                    await sseWriter.WriteChunkAsync(
                        finalSummary,
                        ct);
                }
            }

            await sseWriter.WriteCompletedAsync(
                finalSummary,
                ct);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            await sseWriter.WriteErrorAsync(
                "Request was cancelled",
                ct);

            return Result.Failure(
                new Error(
                    "CANCELLED",
                    "Request was cancelled"));
        }
        catch (Exception ex)
        {
            await sseWriter.WriteErrorAsync(
                ex.Message,
                ct);

            return Result.Failure(
                new Error(
                    "INTERNAL_ERROR",
                    ex.Message));
        }
    }
}