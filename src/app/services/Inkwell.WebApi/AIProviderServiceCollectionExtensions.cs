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
    /// Chat 客户端流水线在默认补丁下使用的 <c>ActivitySource</c> 名称。
    /// 正好命中 <c>Inkwell.ServiceDefaults</c> 中 <c>.AddSource("Inkwell.*")</c> 的前缀过滤
    /// </summary>
    public const string ChatClientTelemetrySourceName = "Inkwell.AI";

    /// <summary>
    /// 默认 Chat 客户端装饰管线：仅开启基于 <see cref="System.Diagnostics.ActivitySource"/> 的 OpenTelemetry
    /// </summary>
    private static ChatClientBuilder DefaultChatPipeline(ChatClientBuilder builder)
        => builder.UseOpenTelemetry(sourceName: ChatClientTelemetrySourceName);

    /// <summary>
    /// 默认 Embedding 客户端装饰管线：仅开启 OpenTelemetry
    /// </summary>
    private static EmbeddingGeneratorBuilder<string, Embedding<float>> DefaultEmbeddingPipeline(
        EmbeddingGeneratorBuilder<string, Embedding<float>> builder)
        => builder.UseOpenTelemetry(sourceName: ChatClientTelemetrySourceName);

    /// <summary>
    /// 根据配置立即实例化所有命名槽位的 <see cref="IChatClient"/> 与 <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>，
    /// 并按 <see cref="AIProviderRoutingOptions"/> 建立逻辑角色 → 物理槽位的映射
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <param name="configuration">应用配置</param>
    /// <param name="configureChatPipeline">
    /// 可选的 Chat 客户端装饰管线配置回调。每个 Chat 槽位的原始 <see cref="IChatClient"/> 会被包成 <see cref="ChatClientBuilder"/>
    /// 交给该回调进行 <c>UseOpenTelemetry</c> / <c>UseLogging</c> / <c>UseFunctionInvocation</c> 等横向能力组装
    /// 。传 <c>null</c> 时使用 <see cref="DefaultChatPipeline"/>（仅启用 OpenTelemetry）
    /// </param>
    /// <param name="configureEmbeddingPipeline">可选的 Embedding 客户端装饰管线配置回调。传 <c>null</c> 时使用 <see cref="DefaultEmbeddingPipeline"/></param>
    /// <returns>Inkwell 核心构建器</returns>
    /// <remarks>
    /// 所有 Provider 实现均为无状态工厂，本方法直接从已注册的 <see cref="IAIChatProvider"/> / <see cref="IAIEmbeddingProvider"/>
    /// 单例实例中查找目标，避免 <c>BuildServiceProvider</c> 反模式（ASP0000）；同时通过 eager 实例化保证
    /// <see cref="ServiceCollectionIntrospectionExtensions.FindKeyedSingletonInstance{T}"/> 能命中注册结果
    /// </remarks>
    public static InkwellCoreBuilder UseAIProviders(
        this InkwellCoreBuilder coreBuilder,
        IConfiguration configuration,
        Func<ChatClientBuilder, ChatClientBuilder>? configureChatPipeline = null,
        Func<EmbeddingGeneratorBuilder<string, Embedding<float>>, EmbeddingGeneratorBuilder<string, Embedding<float>>>? configureEmbeddingPipeline = null)
    {
        IConfigurationSection section = configuration.GetSection(AIProviderOptions.SectionName);
        coreBuilder.Services.Configure<AIProviderOptions>(section);

        AIProviderOptions options = new();
        section.Bind(options);

        IReadOnlyDictionary<string, IAIChatProvider> chatProviders =
            CollectRegisteredProviders<IAIChatProvider>(coreBuilder.Services, p => p.Name);
        IReadOnlyDictionary<string, IAIEmbeddingProvider> embeddingProviders =
            CollectRegisteredProviders<IAIEmbeddingProvider>(coreBuilder.Services, p => p.Name);

        Func<ChatClientBuilder, ChatClientBuilder> chatPipeline = configureChatPipeline ?? DefaultChatPipeline;
        Func<EmbeddingGeneratorBuilder<string, Embedding<float>>, EmbeddingGeneratorBuilder<string, Embedding<float>>> embeddingPipeline =
            configureEmbeddingPipeline ?? DefaultEmbeddingPipeline;

        Dictionary<string, IChatClient> chatInstances = BuildChatInstances(options, chatProviders, chatPipeline);
        RegisterChatSlots(coreBuilder.Services, chatInstances);
        RegisterChatRoleKeys(coreBuilder.Services, chatInstances, options.Routing);

        Dictionary<string, IEmbeddingGenerator<string, Embedding<float>>> embeddingInstances =
            BuildEmbeddingInstances(options, embeddingProviders, embeddingPipeline);
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
    /// 按配置立即构造每个 Chat 命名槽位的 IChatClient 实例，并套用传入的装饰管线
    /// </summary>
    private static Dictionary<string, IChatClient> BuildChatInstances(
        AIProviderOptions options,
        IReadOnlyDictionary<string, IAIChatProvider> providers,
        Func<ChatClientBuilder, ChatClientBuilder> configurePipeline)
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

            IChatClient raw = provider.CreateChatClient(endpoint);
            ChatClientBuilder builder = configurePipeline(raw.AsBuilder());
            instances[slotName] = builder.Build();
        }

        return instances;
    }

    /// <summary>
    /// 按配置立即构造每个 Embedding 命名槽位的生成器实例，并套用传入的装饰管线
    /// </summary>
    private static Dictionary<string, IEmbeddingGenerator<string, Embedding<float>>> BuildEmbeddingInstances(
        AIProviderOptions options,
        IReadOnlyDictionary<string, IAIEmbeddingProvider> providers,
        Func<EmbeddingGeneratorBuilder<string, Embedding<float>>, EmbeddingGeneratorBuilder<string, Embedding<float>>> configurePipeline)
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

            IEmbeddingGenerator<string, Embedding<float>> raw = provider.CreateEmbeddingGenerator(endpoint);
            EmbeddingGeneratorBuilder<string, Embedding<float>> builder = configurePipeline(raw.AsBuilder());
            instances[slotName] = builder.Build();
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
