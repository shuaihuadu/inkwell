using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace Inkwell.AI.AzureOpenAI;

/// <summary>
/// Azure OpenAI Embedding Provider 实现
/// </summary>
internal sealed class AzureOpenAIEmbeddingProvider : IAIEmbeddingProvider
{
    /// <summary>
    /// Provider 名称常量
    /// </summary>
    public const string ProviderName = "AzureOpenAI";

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(AIEndpointOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException($"{ProviderName} embedding provider requires 'Endpoint'.");
        }

        if (string.IsNullOrWhiteSpace(options.Deployment))
        {
            throw new InvalidOperationException($"{ProviderName} embedding provider requires 'Deployment'.");
        }

        AzureOpenAIClient client = AzureOpenAIClientFactory.Create(options);

        return client
            .GetEmbeddingClient(options.Deployment)
            .AsIEmbeddingGenerator();
    }
}
