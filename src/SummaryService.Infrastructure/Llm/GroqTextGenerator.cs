using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Llm;

public sealed class GroqTextGenerator(
    Kernel kernel,
    IOptions<GroqOptions> options,
    ILogger<GroqTextGenerator> logger) : IStreamingTextGenerator
{
    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Generating streaming response with model {Model}", options.Value.Model);

        var function = kernel.CreateFunctionFromPrompt(prompt);
        var result = kernel.InvokeStreamingAsync(function, cancellationToken: ct);

        await foreach (var token in result)
        {
            ct.ThrowIfCancellationRequested();
            yield return token.ToString();
        }
    }
}

public sealed class GroqOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
}
