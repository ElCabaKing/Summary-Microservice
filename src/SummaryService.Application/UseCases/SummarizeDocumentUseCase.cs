using System.Text;
using System.Linq;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Constants;
using SummaryService.Shared.Models;
using SummaryService.Application.Validators;
using SummaryService.Application.Models;

namespace SummaryService.Application.UseCases;

public sealed class SummarizeDocumentUseCase(
    IDocumentParser documentParser,
    ITextChunker textChunker,
    IPromptProvider promptProvider,
    ISummaryGenerator summaryGenerator,
    ISseStreamWriter sseWriter)
{
    public async Task<Result> ExecuteAsync(
        SummaryStreamRequest request,
        CancellationToken ct)
    {
        try
        {
            await sseWriter.WriteStatusAsync("extracting_text", ct);

            var text = await documentParser.ParseAsync(
                request.Document,
                request.ContentType,
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
                var chunkNumber = i + 1;

                await sseWriter.WriteStatusAsync(
                    $"summarizing_chunk_{chunkNumber}_of_{chunks.Count}",
                    ct);

                var chunkPrompt = promptProvider
                    .GetPrompt("summarize-chunk")
                    .Replace("{text}", chunk);

                var aiRequest = new AiRequest
                {
                    Provider = request.Provider,
                    Model = request.Model,
                    Prompt = chunkPrompt
                };


                var chunkSummaryBuilder = new StringBuilder();

                await foreach (var token in summaryGenerator.SummarizeAsync(
                                   aiRequest,
                                   ct))
                {
                    if (string.IsNullOrWhiteSpace(token))
                        continue;

                    chunkSummaryBuilder.Append(token);

                    // Stream each token as it arrives
                    await sseWriter.WriteChunkAsync(token, ct);
                }
                var chunkSummary = chunkSummaryBuilder.ToString().Trim();

                if (!string.IsNullOrWhiteSpace(chunkSummary))
                {
                    chunkSummaries.Add(chunkSummary);
                }

                // Add a separator between chunk summaries
                if (i < chunks.Count - 1)
                {
                    await sseWriter.WriteChunkAsync("\n\n---\n\n", ct);

                    // Rate limiting: Respect Groq's 12,000 TPM limit
                    // 300 token chunks = ~400-500 tokens per request (input + output)
                    // 12,000 / 500 = 24 requests/min max, so 60/24 = 2.5s per request
                    await Task.Delay(3000, ct);
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

                // Check if combined summaries are too large, apply recursive reduction
                finalSummary = await ReduceSummariesAsync(
                      request,
                        combinedSummaries,
                        chunkSummaries.Count,
                        promptProvider,
                        summaryGenerator,
                        sseWriter,
                         ct);
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

    private async Task<string> ReduceSummariesAsync(
       SummaryStreamRequest request,
       string combinedSummaries,
       int summaryCount,
       IPromptProvider promptProvider,
       ISummaryGenerator summaryGenerator,
       ISseStreamWriter sseWriter,
       CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        //
        // If combined summaries fit within reasonable token limit,
        // reduce directly
        //
        const int reduceThreshold = 3000;

        var estimatedTokens = combinedSummaries.Length / 4;

        if (estimatedTokens < reduceThreshold)
        {
            var reducePrompt = promptProvider
                .GetPrompt("reduce")
                .Replace("{text}", combinedSummaries);

            var reduceRequest = new AiRequest
            {
                Provider = request.Provider,
                Model = request.Model,
                Prompt = reducePrompt,
            };

            var finalSummaryBuilder = new StringBuilder();

            await foreach (var token in summaryGenerator.SummarizeAsync(
                               reduceRequest,
                               ct))
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                finalSummaryBuilder.Append(token);

                await sseWriter.WriteChunkAsync(token, ct);
            }

            return finalSummaryBuilder.ToString().Trim();
        }

        //
        // If too large, split into groups
        //
        var summaryLines = combinedSummaries.Split(
            "\n\n",
            StringSplitOptions.RemoveEmptyEntries);

        var groupSize = Math.Max(
            2,
            summaryLines.Length / 3);

        var reducedGroups = new List<string>();

        for (var i = 0; i < summaryLines.Length; i += groupSize)
        {
            ct.ThrowIfCancellationRequested();

            var group = string.Join(
                "\n\n",
                summaryLines
                    .Skip(i)
                    .Take(groupSize));

            var groupReducePrompt = promptProvider
                .GetPrompt("reduce")
                .Replace("{text}", group);

            var groupReduceRequest = new AiRequest
            {
                Provider = request.Provider,
                Model = request.Model,
                Prompt = groupReducePrompt,
            };

            var groupSummaryBuilder = new StringBuilder();

            await foreach (var token in summaryGenerator.SummarizeAsync(
                               groupReduceRequest,
                               ct))
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                groupSummaryBuilder.Append(token);
            }

            var groupSummary = groupSummaryBuilder
                .ToString()
                .Trim();

            if (!string.IsNullOrWhiteSpace(groupSummary))
            {
                reducedGroups.Add(groupSummary);
            }

            //
            // Rate limiting
            //
            await Task.Delay(1500, ct);
        }

        //
        // Recursive reduction
        //
        if (reducedGroups.Count > 1)
        {
            return await ReduceSummariesAsync(
                request,
                string.Join("\n\n", reducedGroups),
                reducedGroups.Count,
                promptProvider,
                summaryGenerator,
                sseWriter,
                ct);
        }

        return reducedGroups.FirstOrDefault()
               ?? string.Empty;
    }
}