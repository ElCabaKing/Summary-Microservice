using System.Text;
using System.Linq;
using SummaryService.Application.Interfaces;
using SummaryService.Shared.Models;
using SummaryService.Application.Models;
using SummaryService.Application.Services;

namespace SummaryService.Application.UseCases;

public sealed class SummarizeDocumentUseCase(
    IDocumentParser documentParser,
    ITextChunker textChunker,
    IStreamingTextGenerator textGenerator,
    ITenantContext tenantContext,
    ISseStreamWriter sseWriter)
{
    public async Task<Result> ExecuteAsync(
        SummaryStreamRequest request,
        CancellationToken ct)
    {
        try
        {
            var tenantId = tenantContext.TenantId;

            if (string.IsNullOrWhiteSpace(tenantId))
                return await FailWithSseError("NO_TENANT", "No se encontró tenant_id en el token", ct).ConfigureAwait(false);

            await sseWriter.WriteStatusAsync("extracting_text", ct).ConfigureAwait(false);

            var text = await documentParser.ParseAsync(
                request.Document,
                request.ContentType,
                ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(text))
                return await FailWithSseError("NO_TEXT", "No text could be extracted from the document", ct).ConfigureAwait(false);

            await sseWriter.WriteStatusAsync("chunking_document", ct).ConfigureAwait(false);

            var chunks = textChunker.Chunk(text, ct);

            if (chunks.Count == 0)
                return await FailWithSseError("EMPTY_DOCUMENT", "Document is empty after chunking", ct).ConfigureAwait(false);

            await sseWriter.WriteStatusAsync("generating_summary", ct).ConfigureAwait(false);

            var chunkSummaries = new List<string>();

            for (var i = 0; i < chunks.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var chunk = chunks[i];
                var chunkNumber = i + 1;

                await sseWriter.WriteStatusAsync(
                    $"summarizing_chunk_{chunkNumber}_of_{chunks.Count}", ct).ConfigureAwait(false);

                var aiRequest = BuildAiRequest(
                    request,
                    Prompts.Get("summarize-chunk").Replace("{text}", chunk));

                var chunkSummaryBuilder = new StringBuilder();

                await foreach (var token in textGenerator.GenerateStreamAsync(aiRequest, ct).ConfigureAwait(false))
                {
                    if (string.IsNullOrWhiteSpace(token))
                        continue;

                    chunkSummaryBuilder.Append(token);
                    await sseWriter.WriteChunkAsync(token, ct).ConfigureAwait(false);
                }

                var chunkSummary = chunkSummaryBuilder.ToString().Trim();

                if (!string.IsNullOrWhiteSpace(chunkSummary))
                    chunkSummaries.Add(chunkSummary);

                if (i < chunks.Count - 1)
                {
                    await sseWriter.WriteChunkAsync("\n\n---\n\n", ct).ConfigureAwait(false);
                    await Task.Delay(3000, ct).ConfigureAwait(false);
                }
            }

            string finalSummary;

            if (chunkSummaries.Count > 1)
            {
                await sseWriter.WriteStatusAsync("reducing_summary", ct).ConfigureAwait(false);

                finalSummary = await ReduceSummariesAsync(
                    request,
                    string.Join("\n\n", chunkSummaries),
                    ct);
            }
            else
            {
                finalSummary = chunkSummaries.FirstOrDefault() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(finalSummary))
                    await sseWriter.WriteChunkAsync(finalSummary, ct).ConfigureAwait(false);
            }

            await sseWriter.WriteCompletedAsync(finalSummary, ct).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return await FailWithSseError("CANCELLED", "Request was cancelled", ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return await FailWithSseError("INTERNAL_ERROR", ex.Message, ct).ConfigureAwait(false);
        }
    }

    private async Task<string> ReduceSummariesAsync(
        SummaryStreamRequest request,
        string combinedSummaries,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        const int reduceThreshold = 3000;
        var estimatedTokens = TokenEstimator.Estimate(combinedSummaries);

        if (estimatedTokens < reduceThreshold)
        {
            var reducePrompt = Prompts.Get("reduce").Replace("{text}", combinedSummaries);
            var reduceRequest = BuildAiRequest(request, reducePrompt);
            var finalSummaryBuilder = new StringBuilder();

            await foreach (var token in textGenerator.GenerateStreamAsync(reduceRequest, ct).ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                finalSummaryBuilder.Append(token);
                await sseWriter.WriteChunkAsync(token, ct).ConfigureAwait(false);
            }

            return finalSummaryBuilder.ToString().Trim();
        }

        var summaryLines = combinedSummaries.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var groupSize = Math.Max(2, summaryLines.Length / 3);
        var reducedGroups = new List<string>();

        for (var i = 0; i < summaryLines.Length; i += groupSize)
        {
            ct.ThrowIfCancellationRequested();

            var group = string.Join("\n\n", summaryLines.Skip(i).Take(groupSize));
            var groupPrompt = Prompts.Get("reduce").Replace("{text}", group);
            var groupRequest = BuildAiRequest(request, groupPrompt);
            var groupSummaryBuilder = new StringBuilder();

            await foreach (var token in textGenerator.GenerateStreamAsync(groupRequest, ct).ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                groupSummaryBuilder.Append(token);
            }

            var groupSummary = groupSummaryBuilder.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(groupSummary))
                reducedGroups.Add(groupSummary);

            await Task.Delay(1500, ct).ConfigureAwait(false);
        }

        if (reducedGroups.Count > 1)
            return await ReduceSummariesAsync(request, string.Join("\n\n", reducedGroups), ct);

        return reducedGroups.FirstOrDefault() ?? string.Empty;
    }

    private static AiRequest BuildAiRequest(SummaryStreamRequest request, string prompt)
    {
        return new AiRequest
        {
            Provider = request.Provider,
            Model = request.Model,
            Prompt = prompt,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens
        };
    }

    private async Task<Result> FailWithSseError(string code, string message, CancellationToken ct)
    {
        await sseWriter.WriteErrorAsync(message, ct).ConfigureAwait(false);
        return Result.Failure(new Error(code, message));
    }
}
