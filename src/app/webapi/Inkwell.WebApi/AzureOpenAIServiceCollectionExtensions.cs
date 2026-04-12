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
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <param name="configuration">应用配置</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseAzureOpenAI(this InkwellCoreBuilder coreBuilder, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(AzureOpenAIOptions.SectionName);
        AzureOpenAIOptions options = new();
        section.Bind(options);

        // 验证必填配置
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException($"Configuration '{AzureOpenAIOptions.SectionName}:Endpoint' is required.");
        }

        if (string.IsNullOrWhiteSpace(options.PrimaryDeploymentName))
        {
            throw new InvalidOperationException($"Configuration '{AzureOpenAIOptions.SectionName}:PrimaryDeploymentName' is required.");
        }

        // 注册配置对象
        coreBuilder.Services.Configure<AzureOpenAIOptions>(section);

        // 创建 OpenAI 客户端（ApiKey 为空时回退到 AzureCliCredential）
        AzureOpenAIClient azureClient = CreateAzureOpenAIClient(options);

        // 创建 Primary IChatClient（写作、审核等高质量任务）
        IChatClient primaryChatClient = azureClient
            .GetChatClient(options.PrimaryDeploymentName)
            .AsIChatClient();

        // 创建 Secondary IChatClient（分析、翻译等经济任务）
        IChatClient secondaryChatClient = azureClient
            .GetChatClient(options.SecondaryDeploymentName)
            .AsIChatClient();

        // 注册到 DI
        coreBuilder.Services.AddKeyedSingleton<IChatClient>(ModelServiceKeys.Primary, primaryChatClient);
        coreBuilder.Services.AddKeyedSingleton<IChatClient>(ModelServiceKeys.Secondary, secondaryChatClient);
        coreBuilder.Services.AddSingleton(primaryChatClient);

        return coreBuilder;
    }

    /// <summary>
    /// 创建 Azure OpenAI 客户端
    /// </summary>
    private static AzureOpenAIClient CreateAzureOpenAIClient(AzureOpenAIOptions options)
    {
        Uri endpoint = new(options.Endpoint);

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return new AzureOpenAIClient(endpoint, new AzureKeyCredential(options.ApiKey));
        }

        return new AzureOpenAIClient(endpoint, new AzureCliCredential());
    }
}
