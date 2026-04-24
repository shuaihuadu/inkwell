using Inkwell.WebApi.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// 多 AI Provider 的服务注册扩展方法
/// </summary>
/// <remarks>
/// 本类是 Step 2/3 引入的新注册入口。相对于旧的 <see cref="AzureOpenAIServiceCollectionExtensions.UseAzureOpenAI"/>，它具备以下差异：
/// <list type="bullet">
///   <item>Chat 配置从单一 Provider 扩展为命名槽位 + Provider 名路由，支持 Azure OpenAI / OpenAI 官方 / 任意 OpenAI 兼容协议混用</item>
///   <item>按 <see cref="AIProviderRoutingOptions"/> 建立逻辑角色 → 物理槽位的映射</item>
///   <item>所有 <see cref="IChatClient"/> / <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> 在 <c>UseAIProviders</c>
///         调用期间立即实例化（eager），以兼容基于 <see cref="ServiceCollectionIntrospectionExtensions.FindKeyedSingletonInstance{T}"/>
///         的下游注册扩展（<c>AddInkwellAgents</c> / <c>AddInkwellWorkflows</c>）</item>
///   <item>额外以 <see cref="ModelServiceKeys"/> 定义的旧键同时注册 Primary / Secondary / Embedding，保证向后兼容</item>
/// </list>
/// </remarks>
public static class AIProviderServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Azure OpenAI Chat 与 Embedding Provider 到 DI 容器（以实例形式注册，便于 <see cref="UseAIProviders"/> 内省）
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder AddAzureOpenAIProvider(this InkwellCoreBuilder coreBuilder)
    {
        coreBuilder.Services.AddSingleton<IAIChatProvider>(new AzureOpenAIChatProvider());
        coreBuilder.Services.AddSingleton<IAIEmbeddingProvider>(new AzureOpenAIEmbeddingProvider());
        return coreBuilder;
    }

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

    /// <summary>
    /// 根据配置立即实例化所有命名槽位的 <see cref="IChatClient"/> 与 <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>，
    /// 并按 <see cref="AIProviderRoutingOptions"/> 建立逻辑角色 → 物理槽位的映射
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <param name="configuration">应用配置</param>
    /// <returns>Inkwell 核心构建器</returns>
    /// <remarks>
    /// 所有 Provider 实现均为无状态工厂，本方法直接从已注册的 <see cref="IAIChatProvider"/> / <see cref="IAIEmbeddingProvider"/>
    /// 单例实例中查找目标，避免 <c>BuildServiceProvider</c> 反模式（ASP0000）；同时通过 eager 实例化保证
    /// <see cref="ServiceCollectionIntrospectionExtensions.FindKeyedSingletonInstance{T}"/> 能命中注册结果
    /// </remarks>
    public static InkwellCoreBuilder UseAIProviders(this InkwellCoreBuilder coreBuilder, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(AIProviderOptions.SectionName);
        coreBuilder.Services.Configure<AIProviderOptions>(section);

        AIProviderOptions options = new();
        section.Bind(options);

        IReadOnlyDictionary<string, IAIChatProvider> chatProviders =
            CollectRegisteredProviders<IAIChatProvider>(coreBuilder.Services, p => p.Name);
        IReadOnlyDictionary<string, IAIEmbeddingProvider> embeddingProviders =
            CollectRegisteredProviders<IAIEmbeddingProvider>(coreBuilder.Services, p => p.Name);

        Dictionary<string, IChatClient> chatInstances = BuildChatInstances(options, chatProviders);
        RegisterChatSlots(coreBuilder.Services, chatInstances);
        RegisterChatRoleKeys(coreBuilder.Services, chatInstances, options.Routing);

        Dictionary<string, IEmbeddingGenerator<string, Embedding<float>>> embeddingInstances =
            BuildEmbeddingInstances(options, embeddingProviders);
        RegisterEmbeddingSlots(coreBuilder.Services, embeddingInstances, options.Routing);

        return coreBuilder;
    }

    /// <summary>
    /// 从 IServiceCollection 中收集所有以实例形式注册的某类 Provider，按 Name 建立 OrdinalIgnoreCase 查表
    /// </summary>
    private static IReadOnlyDictionary<string, T> CollectRegisteredProviders<T>(
        IServiceCollection services,
        Func<T, string> nameSelector) where T : class
    {
        Dictionary<string, T> map = new(StringComparer.OrdinalIgnoreCase);

        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(T)
                && !descriptor.IsKeyedService
                && descriptor.ImplementationInstance is T instance)
            {
                map[nameSelector(instance)] = instance;
            }
        }

        return map;
    }

    /// <summary>
    /// 按配置立即构造每个 Chat 命名槽位的 IChatClient 实例
    /// </summary>
    private static Dictionary<string, IChatClient> BuildChatInstances(
        AIProviderOptions options,
        IReadOnlyDictionary<string, IAIChatProvider> providers)
    {
        Dictionary<string, IChatClient> instances = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string slotName, AIEndpointOptions endpoint) in options.Chat)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Provider))
            {
                throw new InvalidOperationException(
                    $"Configuration '{AIProviderOptions.SectionName}:Chat:{slotName}:Provider' is required.");
            }

            if (!providers.TryGetValue(endpoint.Provider, out IAIChatProvider? provider))
            {
                throw new InvalidOperationException(
                    $"Unknown AI chat provider '{endpoint.Provider}' for slot '{slotName}'. " +
                    $"Did you forget to call AddXxxProvider() before UseAIProviders()?");
            }

            instances[slotName] = provider.CreateChatClient(endpoint);
        }

        return instances;
    }

    /// <summary>
    /// 按配置立即构造每个 Embedding 命名槽位的生成器实例
    /// </summary>
    private static Dictionary<string, IEmbeddingGenerator<string, Embedding<float>>> BuildEmbeddingInstances(
        AIProviderOptions options,
        IReadOnlyDictionary<string, IAIEmbeddingProvider> providers)
    {
        Dictionary<string, IEmbeddingGenerator<string, Embedding<float>>> instances =
            new(StringComparer.OrdinalIgnoreCase);

        foreach ((string slotName, AIEndpointOptions endpoint) in options.Embedding)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Provider))
            {
                throw new InvalidOperationException(
                    $"Configuration '{AIProviderOptions.SectionName}:Embedding:{slotName}:Provider' is required.");
            }

            if (!providers.TryGetValue(endpoint.Provider, out IAIEmbeddingProvider? provider))
            {
                throw new InvalidOperationException(
                    $"Unknown AI embedding provider '{endpoint.Provider}' for slot '{slotName}'.");
            }

            instances[slotName] = provider.CreateEmbeddingGenerator(endpoint);
        }

        return instances;
    }

    /// <summary>
    /// 按命名槽位注册每个 IChatClient 为 Keyed Singleton 实例
    /// </summary>
    private static void RegisterChatSlots(IServiceCollection services, Dictionary<string, IChatClient> instances)
    {
        foreach ((string slotName, IChatClient client) in instances)
        {
            services.AddKeyedSingleton<IChatClient>(slotName, client);
        }
    }

    /// <summary>
    /// 按 Routing 把逻辑角色键同时注册为 <see cref="AIProviderKeys"/>（新）与 <see cref="ModelServiceKeys"/>（旧，保持向后兼容）
    /// </summary>
    private static void RegisterChatRoleKeys(
        IServiceCollection services,
        Dictionary<string, IChatClient> instances,
        AIProviderRoutingOptions routing)
    {
        if (!instances.TryGetValue(routing.Primary, out IChatClient? primary))
        {
            throw new InvalidOperationException(
                $"AIProviders.Routing.Primary points to '{routing.Primary}', but no chat slot with that name is configured. " +
                $"Define it under '{AIProviderOptions.SectionName}:Chat:{routing.Primary}'.");
        }

        services.AddKeyedSingleton<IChatClient>(AIProviderKeys.Primary, primary);
#pragma warning disable CS0618 // 向后兼容：保留对 ModelServiceKeys 的注册
        services.AddKeyedSingleton<IChatClient>(ModelServiceKeys.Primary, primary);
#pragma warning restore CS0618
        services.AddSingleton(primary);

        IChatClient secondary = instances.TryGetValue(routing.Secondary, out IChatClient? s) ? s : primary;
        services.AddKeyedSingleton<IChatClient>(AIProviderKeys.Secondary, secondary);
#pragma warning disable CS0618
        services.AddKeyedSingleton<IChatClient>(ModelServiceKeys.Secondary, secondary);
#pragma warning restore CS0618

        IChatClient title = instances.TryGetValue(routing.Title, out IChatClient? t) ? t : primary;
        services.AddKeyedSingleton<IChatClient>(AIProviderKeys.Title, title);
    }

    /// <summary>
    /// 按命名槽位注册每个 Embedding 生成器为 Keyed Singleton；Routing.Embedding 指向的槽位额外注册为非 Keyed 单例与旧 <see cref="ModelServiceKeys.Embedding"/>
    /// </summary>
    private static void RegisterEmbeddingSlots(
        IServiceCollection services,
        Dictionary<string, IEmbeddingGenerator<string, Embedding<float>>> instances,
        AIProviderRoutingOptions routing)
    {
        foreach ((string slotName, IEmbeddingGenerator<string, Embedding<float>> generator) in instances)
        {
            services.AddKeyedSingleton(slotName, generator);
        }

        if (instances.Count == 0)
        {
            return;
        }

        if (!instances.TryGetValue(routing.Embedding, out IEmbeddingGenerator<string, Embedding<float>>? defaultEmbedding))
        {
            return;
        }

        services.AddSingleton(defaultEmbedding);
#pragma warning disable CS0618
        services.AddKeyedSingleton(ModelServiceKeys.Embedding, defaultEmbedding);
#pragma warning restore CS0618
    }
}
