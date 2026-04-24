using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Inkwell.AI.OpenAI;

/// <summary>
/// OpenAI 官方 Chat Provider 实现（使用官方 api.openai.com 端点的便捷包装）
/// </summary>
internal sealed class OpenAIChatProvider : IAIChatProvider
{
    /// <summary>
    /// Provider 名称常量
    /// </summary>
    public const string ProviderName = "OpenAI";

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public IChatClient CreateChatClient(AIEndpointOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException($"{ProviderName} provider requires 'ApiKey'.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new InvalidOperationException($"{ProviderName} provider requires 'Model'.");
        }

        OpenAIClient client = new(new ApiKeyCredential(options.ApiKey));

        return client
            .GetChatClient(options.Model)
            .AsIChatClient();
    }
}
