using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// Azure OpenAI 服务注册扩展方法
/// </summary>
public static class AzureOpenAIServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Azure OpenAI 多模型服务（Primary / Secondary / Embedding）
    /// 每个模型可独立配置 Endpoint、ApiKey、DeploymentName
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <param name="configuration">应用配置</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseAzureOpenAI(this InkwellCoreBuilder coreBuilder, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(AzureOpenAIOptions.SectionName);
        AzureOpenAIOptions options = new();
        section.Bind(options);

        // 验证 Primary 必填
        if (string.IsNullOrWhiteSpace(options.Primary.Endpoint))
        {
            throw new InvalidOperationException($"Configuration '{AzureOpenAIOptions.SectionName}:Primary:Endpoint' is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Primary.DeploymentName))
        {
            throw new InvalidOperationException($"Configuration '{AzureOpenAIOptions.SectionName}:Primary:DeploymentName' is required.");
        }

        // 注册配置对象
        coreBuilder.Services.Configure<AzureOpenAIOptions>(section);

        // 创建 Primary IChatClient
        IChatClient primaryChatClient = CreateChatClient(options.Primary);
        coreBuilder.Services.AddKeyedSingleton<IChatClient>(ModelServiceKeys.Primary, primaryChatClient);
        coreBuilder.Services.AddSingleton(primaryChatClient);

        // 创建 Secondary IChatClient（未配置时回退到 Primary）
        AzureOpenAIModelOptions secondaryConfig = FallbackTo(options.Secondary, options.Primary);
        IChatClient secondaryChatClient = CreateChatClient(secondaryConfig);
        coreBuilder.Services.AddKeyedSingleton<IChatClient>(ModelServiceKeys.Secondary, secondaryChatClient);

        return coreBuilder;
    }

    /// <summary>
    /// 根据模型配置创建 IChatClient
    /// </summary>
    private static IChatClient CreateChatClient(AzureOpenAIModelOptions modelOptions)
    {
        AzureOpenAIClient azureClient = CreateAzureOpenAIClient(modelOptions);

        return azureClient
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
    /// 若目标配置的 Endpoint 或 DeploymentName 为空，则继承 fallback 的值
    /// </summary>
    private static AzureOpenAIModelOptions FallbackTo(AzureOpenAIModelOptions target, AzureOpenAIModelOptions fallback)
    {
        return new AzureOpenAIModelOptions
        {
            Endpoint = string.IsNullOrWhiteSpace(target.Endpoint) ? fallback.Endpoint : target.Endpoint,
            ApiKey = string.IsNullOrWhiteSpace(target.ApiKey) ? fallback.ApiKey : target.ApiKey,
            DeploymentName = string.IsNullOrWhiteSpace(target.DeploymentName) ? fallback.DeploymentName : target.DeploymentName
        };
    }
}
