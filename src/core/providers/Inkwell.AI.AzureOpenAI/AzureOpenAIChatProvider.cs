using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace Inkwell.AI.AzureOpenAI;

/// <summary>
/// Azure OpenAI Chat Provider 实现
/// </summary>
internal sealed class AzureOpenAIChatProvider : IAIChatProvider
{
    /// <summary>
    /// Provider 名称常量
    /// </summary>
    public const string ProviderName = "AzureOpenAI";

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public IChatClient CreateChatClient(AIEndpointOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException($"{ProviderName} provider requires 'Endpoint'.");
        }

        if (string.IsNullOrWhiteSpace(options.Deployment))
        {
            throw new InvalidOperationException($"{ProviderName} provider requires 'Deployment'.");
        }

        AzureOpenAIClient client = AzureOpenAIClientFactory.Create(options);

        return client
            .GetChatClient(options.Deployment)
            .AsIChatClient();
    }
}
