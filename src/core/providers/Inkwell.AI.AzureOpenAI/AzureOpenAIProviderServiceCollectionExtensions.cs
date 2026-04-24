using Inkwell.AI.AzureOpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// Azure OpenAI Provider 在 DI 容器中的注册扩展方法
/// </summary>
public static class AzureOpenAIProviderServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Azure OpenAI Chat 与 Embedding Provider 到 DI 容器（以实例形式注册，便于 <c>UseAIProviders</c> 内省）
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder AddAzureOpenAIProvider(this InkwellCoreBuilder coreBuilder)
    {
        coreBuilder.Services.AddSingleton<IAIChatProvider>(new AzureOpenAIChatProvider());
        coreBuilder.Services.AddSingleton<IAIEmbeddingProvider>(new AzureOpenAIEmbeddingProvider());
        return coreBuilder;
    }
}
