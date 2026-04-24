using Inkwell.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// OpenAI 及 OpenAI 兼容协议 Provider 在 DI 容器中的注册扩展方法
/// </summary>
public static class OpenAIProviderServiceCollectionExtensions
{
    /// <summary>
    /// 注册 OpenAI 官方与 OpenAI 兼容协议 Chat Provider 到 DI 容器（以实例形式注册）
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder AddOpenAIProvider(this InkwellCoreBuilder coreBuilder)
    {
        coreBuilder.Services.AddSingleton<IAIChatProvider>(new OpenAIChatProvider());
        coreBuilder.Services.AddSingleton<IAIChatProvider>(new OpenAICompatibleChatProvider());
        return coreBuilder;
    }
}
