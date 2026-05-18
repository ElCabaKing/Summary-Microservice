using System.Runtime.CompilerServices;
using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Llm;

public sealed class SummaryGenerator(IStreamingTextGenerator textGenerator) : ISummaryGenerator
{
    public async IAsyncEnumerable<string> SummarizeAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var token in textGenerator.GenerateStreamAsync(prompt, ct))
        {
            yield return token;
        }
    }
}
