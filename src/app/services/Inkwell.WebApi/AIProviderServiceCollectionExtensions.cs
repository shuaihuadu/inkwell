using Inkwell.WebApi.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>
/// 多 AI Provider 的服务注册扩展方法
/// </summary>
public static class AIProviderServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Azure OpenAI Chat 与 Embedding Provider 到 DI 容器
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder AddAzureOpenAIProvider(this InkwellCoreBuilder coreBuilder)
    {
        coreBuilder.Services.AddSingleton<IAIChatProvider, AzureOpenAIChatProvider>();
        coreBuilder.Services.AddSingleton<IAIEmbeddingProvider, AzureOpenAIEmbeddingProvider>();
        return coreBuilder;
    }

    /// <summary>
    /// 注册 OpenAI 官方与 OpenAI 兼容协议 Chat Provider 到 DI 容器
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder AddOpenAIProvider(this InkwellCoreBuilder coreBuilder)
    {
        coreBuilder.Services.AddSingleton<IAIChatProvider, OpenAIChatProvider>();
        coreBuilder.Services.AddSingleton<IAIChatProvider, OpenAICompatibleChatProvider>();
        return coreBuilder;
    }

    /// <summary>
    /// 根据配置实例化所有命名槽位的 <see cref="IChatClient"/> 与 <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>，
    /// 并按 <see cref="AIProviderRoutingOptions"/> 建立逻辑角色 → 物理槽位的映射
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <param name="configuration">应用配置</param>
    /// <returns>Inkwell 核心构建器</returns>
    /// <remarks>
    /// 所有 Provider 的查表与实例化都放在 Keyed 工厂委托中，由最终的 <see cref="IServiceProvider"/> 在首次 resolve
    /// 时驱动，避免 <c>BuildServiceProvider</c> 反模式（ASP0000）
    /// </remarks>
    public static InkwellCoreBuilder UseAIProviders(this InkwellCoreBuilder coreBuilder, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(AIProviderOptions.SectionName);
        coreBuilder.Services.Configure<AIProviderOptions>(section);

        AIProviderOptions options = new();
        section.Bind(options);

        RegisterChatClients(coreBuilder.Services, options);
        RegisterChatRoleKeys(coreBuilder.Services);
        RegisterEmbeddings(coreBuilder.Services, options);

        return coreBuilder;
    }

    /// <summary>
    /// 为每个 Chat 命名槽位注册一个 Keyed Singleton，Provider 在工厂内从 <see cref="IServiceProvider"/> 中解析
    /// </summary>
    private static void RegisterChatClients(IServiceCollection services, AIProviderOptions options)
    {
        foreach ((string name, AIEndpointOptions endpoint) in options.Chat)
        {
            AIEndpointOptions slot = endpoint;
            string slotName = name;

            services.AddKeyedSingleton<IChatClient>(slotName, (sp, _) =>
            {
                IAIChatProvider provider = ResolveChatProvider(sp, slot.Provider, slotName);
                return provider.CreateChatClient(slot);
            });
        }
    }

    /// <summary>
    /// 把逻辑角色键（Primary / Secondary / Title）通过 Routing 映射到具体命名槽位
    /// </summary>
    private static void RegisterChatRoleKeys(IServiceCollection services)
    {
        services.AddKeyedSingleton<IChatClient>(AIProviderKeys.Primary, (sp, _) =>
            sp.GetRequiredKeyedService<IChatClient>(
                sp.GetRequiredService<IOptions<AIProviderOptions>>().Value.Routing.Primary));

        services.AddKeyedSingleton<IChatClient>(AIProviderKeys.Secondary, (sp, _) =>
            sp.GetRequiredKeyedService<IChatClient>(
                sp.GetRequiredService<IOptions<AIProviderOptions>>().Value.Routing.Secondary));

        services.AddKeyedSingleton<IChatClient>(AIProviderKeys.Title, (sp, _) =>
            sp.GetRequiredKeyedService<IChatClient>(
                sp.GetRequiredService<IOptions<AIProviderOptions>>().Value.Routing.Title));
    }

    /// <summary>
    /// 为每个 Embedding 命名槽位注册 Keyed Singleton，并把 Routing.Embedding 指向的槽位额外注册为非 Keyed 单例（向后兼容）
    /// </summary>
    private static void RegisterEmbeddings(IServiceCollection services, AIProviderOptions options)
    {
        foreach ((string name, AIEndpointOptions endpoint) in options.Embedding)
        {
            AIEndpointOptions slot = endpoint;
            string slotName = name;

            services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(slotName, (sp, _) =>
            {
                IAIEmbeddingProvider provider = ResolveEmbeddingProvider(sp, slot.Provider, slotName);
                return provider.CreateEmbeddingGenerator(slot);
            });
        }

        if (options.Embedding.Count == 0)
        {
            return;
        }

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            sp.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>(
                sp.GetRequiredService<IOptions<AIProviderOptions>>().Value.Routing.Embedding));
    }

    private static IAIChatProvider ResolveChatProvider(IServiceProvider sp, string providerName, string slotName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException(
                $"Configuration '{AIProviderOptions.SectionName}:Chat:{slotName}:Provider' is required.");
        }

        IAIChatProvider? provider = sp.GetServices<IAIChatProvider>()
            .FirstOrDefault(p => string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase));

        return provider
            ?? throw new InvalidOperationException(
                $"Unknown AI chat provider '{providerName}' for slot '{slotName}'. " +
                $"Did you forget to call AddXxxProvider() before UseAIProviders()?");
    }

    private static IAIEmbeddingProvider ResolveEmbeddingProvider(IServiceProvider sp, string providerName, string slotName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException(
                $"Configuration '{AIProviderOptions.SectionName}:Embedding:{slotName}:Provider' is required.");
        }

        IAIEmbeddingProvider? provider = sp.GetServices<IAIEmbeddingProvider>()
            .FirstOrDefault(p => string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase));

        return provider
            ?? throw new InvalidOperationException(
                $"Unknown AI embedding provider '{providerName}' for slot '{slotName}'.");
    }
}
