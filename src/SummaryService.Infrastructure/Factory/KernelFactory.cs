using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SummaryService.Domain.Options;

using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Factory;

public sealed class KernelFactory(
    IOptions<AiOptions> options)
    : IKernelFactory
{
    private readonly AiOptions _options = options.Value;
    private static readonly ConcurrentDictionary<(string Provider, string Model), Kernel> _cache = new();

    public Kernel Create(
        string provider,
        string model)
    {
        var key = (provider, model);

        return _cache.GetOrAdd(key, _ =>
        {
            if (!_options.Providers.TryGetValue(provider, out var config))
            {
                throw new InvalidOperationException(
                    $"Provider '{provider}' not configured");
            }

            var resolvedKey = config.ApiKey
                ?? throw new InvalidOperationException(
                    $"No API key available for provider '{provider}'");

            var builder = Kernel.CreateBuilder();

            switch (config.Type.ToLowerInvariant())
            {
                case "openai":
                case "openaicompatible":

                    builder.AddOpenAIChatCompletion(
                        modelId: model,
                        apiKey: resolvedKey,
                        endpoint: new Uri(config.Endpoint));

                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported provider '{config.Type}'");
            }

            return builder.Build();
        });
    }
}
