using System.Runtime.CompilerServices;
using SummaryService.Application.Interfaces;
using SummaryService.Application.Models;

namespace SummaryService.Infrastructure.Llm;

public sealed class SummaryGenerator(IStreamingTextGenerator textGenerator) : ISummaryGenerator
{
    public async IAsyncEnumerable<string> SummarizeAsync(
        AiRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var token in textGenerator.GenerateStreamAsync(request, ct))
        {
            yield return token;
        }
    }
}
