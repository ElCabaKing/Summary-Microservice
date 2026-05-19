using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SummaryService.Infrastructure.Llm.Options;

namespace SummaryService.Infrastructure.Factory;

public sealed class KernelFactory(
    IOptions<AiOptions> options)
    : IKernelFactory
{
    private readonly AiOptions _options = options.Value;

    public Kernel Create(
        string provider,
        string model)
    {
        if (!_options.Providers.TryGetValue(provider, out var config))
        {
            throw new InvalidOperationException(
                $"Provider '{provider}' not configured");
        }

        var builder = Kernel.CreateBuilder();

        switch (config.Type.ToLowerInvariant())
        {
            case "openai":
            case "openaicompatible":

                builder.AddOpenAIChatCompletion(
                    modelId: model,
                    apiKey: config.ApiKey!,
                    endpoint: new Uri(config.Endpoint));

                break;


            default:
                throw new InvalidOperationException(
                    $"Unsupported provider '{config.Type}'");
        }

        return builder.Build();
    }
}