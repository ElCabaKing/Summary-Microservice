using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SummaryService.Application.Interfaces;
using SummaryService.Application.Models;
using SummaryService.Infrastructure.Factory;

namespace SummaryService.Infrastructure.Llm;

public sealed class SemanticKernelStreamingTextGenerator(
    IKernelFactory kernelFactory,
    ILogger<SemanticKernelStreamingTextGenerator> logger)
    : IStreamingTextGenerator
{
    public async IAsyncEnumerable<string> GenerateStreamAsync(
        AiRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Generating streaming response using Provider={Provider}, Model={Model}",
            request.Provider,
            request.Model);

        var kernel = kernelFactory.Create(
            request.Provider,
            request.Model,
            request.ApiKey);

        var function = kernel.CreateFunctionFromPrompt(request.Prompt);

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens
        };

        var arguments = new KernelArguments(executionSettings);

        var result = kernel.InvokeStreamingAsync(
            function,
            arguments,
            cancellationToken: ct);

        await foreach (var token in result)
        {
            ct.ThrowIfCancellationRequested();

            var text = token.ToString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }
        }
    }
}
