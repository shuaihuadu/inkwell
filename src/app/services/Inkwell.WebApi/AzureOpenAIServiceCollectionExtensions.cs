using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Inkwell;

/// <summary>
/// 模型服务注册扩展方法。支持 Azure OpenAI 与 OpenAI 兼容协议（DeepSeek / Qwen / Moonshot / 智谱 / OpenAI 官方等）两种 Provider
/// </summary>
/// <remarks>
/// 扩展方法名沿用 UseAzureOpenAI 以保持向后兼容；Provider 的具体类型由 <see cref="AzureOpenAIModelOptions.Provider"/> 字段决定
/// </remarks>
public static class AzureOpenAIServiceCollectionExtensions
{
    /// <summary>
    /// 注册多模型服务（Primary / Secondary）。每个模型可独立配置 Provider、Endpoint、ApiKey、DeploymentName
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <param name="configuration">应用配置</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseAzureOpenAI(this InkwellCoreBuilder coreBuilder, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(AzureOpenAIOptions.SectionName);
        AzureOpenAIOptions options = new();
        section.Bind(options);

        ValidateModel(options.Primary, $"{AzureOpenAIOptions.SectionName}:Primary");

        coreBuilder.Services.Configure<AzureOpenAIOptions>(section);

        IChatClient primaryChatClient = CreateChatClient(options.Primary);
#pragma warning disable CS0618 // 老入口保留对 ModelServiceKeys 的直接引用，新代码应使用 UseAIProviders
        coreBuilder.Services.AddKeyedSingleton<IChatClient>(ModelServiceKeys.Primary, primaryChatClient);
        coreBuilder.Services.AddSingleton(primaryChatClient);

        AzureOpenAIModelOptions secondaryConfig = FallbackTo(options.Secondary, options.Primary);
        IChatClient secondaryChatClient = CreateChatClient(secondaryConfig);
        coreBuilder.Services.AddKeyedSingleton<IChatClient>(ModelServiceKeys.Secondary, secondaryChatClient);
#pragma warning restore CS0618

        return coreBuilder;
    }

    /// <summary>
    /// 注册 Embedding 生成器。Embedding 当前仅支持 Azure OpenAI Provider，配置回退到 Primary 的 Endpoint/ApiKey
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <param name="configuration">应用配置</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseAzureOpenAIEmbedding(this InkwellCoreBuilder coreBuilder, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(AzureOpenAIOptions.SectionName);
        AzureOpenAIOptions options = new();
        section.Bind(options);

        AzureOpenAIModelOptions embeddingConfig = FallbackTo(options.Embedding, options.Primary);

        if (string.IsNullOrWhiteSpace(embeddingConfig.Endpoint) ||
            string.IsNullOrWhiteSpace(embeddingConfig.DeploymentName))
        {
            return coreBuilder;
        }

        // Embedding 目前只支持 Azure OpenAI；即便 Primary 是 OpenAICompatible，Embedding 也单独走 Azure
        coreBuilder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            AzureOpenAIClient client = CreateAzureOpenAIClient(embeddingConfig);
            return client
                .GetEmbeddingClient(embeddingConfig.DeploymentName)
                .AsIEmbeddingGenerator();
        });

        return coreBuilder;
    }

    /// <summary>
    /// 按 Provider 分流创建 IChatClient
    /// </summary>
    private static IChatClient CreateChatClient(AzureOpenAIModelOptions modelOptions)
    {
        string provider = string.IsNullOrWhiteSpace(modelOptions.Provider)
            ? ModelProviderTypes.AzureOpenAI
            : modelOptions.Provider.Trim();

        return provider switch
        {
            _ when string.Equals(provider, ModelProviderTypes.AzureOpenAI, StringComparison.OrdinalIgnoreCase)
                => CreateAzureOpenAIChatClient(modelOptions),
            _ when string.Equals(provider, ModelProviderTypes.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
                => CreateOpenAICompatibleChatClient(modelOptions),
            _ => throw new InvalidOperationException(
                $"Unknown model provider '{modelOptions.Provider}'. Supported values: " +
                $"'{ModelProviderTypes.AzureOpenAI}', '{ModelProviderTypes.OpenAICompatible}'.")
        };
    }

    /// <summary>
    /// 创建 Azure OpenAI IChatClient
    /// </summary>
    private static IChatClient CreateAzureOpenAIChatClient(AzureOpenAIModelOptions modelOptions)
    {
        AzureOpenAIClient azureClient = CreateAzureOpenAIClient(modelOptions);
        return azureClient
            .GetChatClient(modelOptions.DeploymentName)
            .AsIChatClient();
    }

    /// <summary>
    /// 创建 OpenAI 兼容协议的 IChatClient（DeepSeek / Qwen / Moonshot / 智谱 / OpenAI 官方等）
    /// </summary>
    private static IChatClient CreateOpenAICompatibleChatClient(AzureOpenAIModelOptions modelOptions)
    {
        if (string.IsNullOrWhiteSpace(modelOptions.Endpoint))
        {
            throw new InvalidOperationException(
                $"OpenAICompatible provider requires 'Endpoint' (base URL, e.g. 'https://api.deepseek.com/v1').");
        }

        if (string.IsNullOrWhiteSpace(modelOptions.ApiKey))
        {
            throw new InvalidOperationException("OpenAICompatible provider requires 'ApiKey'.");
        }

        if (string.IsNullOrWhiteSpace(modelOptions.DeploymentName))
        {
            throw new InvalidOperationException("OpenAICompatible provider requires 'DeploymentName' (model name).");
        }

        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = new Uri(modelOptions.Endpoint)
        };

        OpenAIClient openAIClient = new(new ApiKeyCredential(modelOptions.ApiKey), clientOptions);

        return openAIClient
            .GetChatClient(modelOptions.DeploymentName)
            .AsIChatClient();
    }

    /// <summary>
    /// 创建 Azure OpenAI 客户端（ApiKey 为空时回退到 AzureCliCredential）
    /// </summary>
    private static AzureOpenAIClient CreateAzureOpenAIClient(AzureOpenAIModelOptions modelOptions)
    {
        Uri endpoint = new(modelOptions.Endpoint);

        if (!string.IsNullOrWhiteSpace(modelOptions.ApiKey))
        {
            return new AzureOpenAIClient(endpoint, new AzureKeyCredential(modelOptions.ApiKey));
        }

        return new AzureOpenAIClient(endpoint, new AzureCliCredential());
    }

    /// <summary>
    /// 校验必填字段
    /// </summary>
    private static void ValidateModel(AzureOpenAIModelOptions model, string path)
    {
        if (string.IsNullOrWhiteSpace(model.Endpoint))
        {
            throw new InvalidOperationException($"Configuration '{path}:Endpoint' is required.");
        }

        if (string.IsNullOrWhiteSpace(model.DeploymentName))
        {
            throw new InvalidOperationException($"Configuration '{path}:DeploymentName' is required.");
        }
    }

    /// <summary>
    /// 若目标配置的 Provider / Endpoint / DeploymentName 为空，则继承 fallback 的值
    /// </summary>
    private static AzureOpenAIModelOptions FallbackTo(AzureOpenAIModelOptions target, AzureOpenAIModelOptions fallback)
    {
        return new AzureOpenAIModelOptions
        {
            Provider = string.IsNullOrWhiteSpace(target.Provider) ? fallback.Provider : target.Provider,
            Endpoint = string.IsNullOrWhiteSpace(target.Endpoint) ? fallback.Endpoint : target.Endpoint,
            ApiKey = string.IsNullOrWhiteSpace(target.ApiKey) ? fallback.ApiKey : target.ApiKey,
            DeploymentName = string.IsNullOrWhiteSpace(target.DeploymentName) ? fallback.DeploymentName : target.DeploymentName
        };
    }
}
