using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Inkwell.WebApi.Providers;

/// <summary>
/// OpenAI 兼容协议 Chat Provider 实现
/// </summary>
/// <remarks>
/// 覆盖 OpenAI 官方、DeepSeek、Qwen、Moonshot、智谱 GLM、Groq、Together、OpenRouter 等所有实现
/// OpenAI /v1/chat/completions 协议的端点。通过 <see cref="AIEndpointOptions.BaseUrl"/> 指定基地址，
/// <see cref="AIEndpointOptions.Model"/> 指定模型名
/// </remarks>
internal sealed class OpenAICompatibleChatProvider : IAIChatProvider
{
    /// <summary>
    /// Provider 名称常量
    /// </summary>
    public const string ProviderName = "OpenAICompatible";

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public IChatClient CreateChatClient(AIEndpointOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException(
                $"{ProviderName} provider requires 'BaseUrl' (e.g. 'https://api.deepseek.com/v1').");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException($"{ProviderName} provider requires 'ApiKey'.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new InvalidOperationException($"{ProviderName} provider requires 'Model'.");
        }

        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = new Uri(options.BaseUrl)
        };

        OpenAIClient client = new(new ApiKeyCredential(options.ApiKey), clientOptions);

        return client
            .GetChatClient(options.Model)
            .AsIChatClient();
    }
}

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
