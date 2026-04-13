using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace Inkwell.Agents;

/// <summary>
/// Agent 服务注册扩展方法
/// </summary>
public static class AgentServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Inkwell 默认 Agent 集合（从配置创建多模型客户端）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置，从中读取已注册的 Keyed IChatClient</param>
    /// <returns>Agent 注册表</returns>
    public static AgentRegistry AddInkwellAgents(this IServiceCollection services, IConfiguration configuration)
    {
        // 从已注册的服务中查找 UseAzureOpenAI 存储的 IChatClient 实例
        // 通过 ServiceDescriptor 遍历找到 Keyed 注册
        IChatClient? primaryClient = null;
        IChatClient? secondaryClient = null;

        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(IChatClient) && descriptor.IsKeyedService)
            {
                if (descriptor.ServiceKey is string key)
                {
                    if (key == ModelServiceKeys.Primary && descriptor.KeyedImplementationInstance is IChatClient primary)
                    {
                        primaryClient = primary;
                    }
                    else if (key == ModelServiceKeys.Secondary && descriptor.KeyedImplementationInstance is IChatClient secondary)
                    {
                        secondaryClient = secondary;
                    }
                }
            }
        }

        if (primaryClient is null)
        {
            throw new InvalidOperationException(
                "Primary IChatClient not found. Call UseAzureOpenAI() before AddInkwellAgents().");
        }

        // 若 Secondary 未配置，回退到 Primary
        secondaryClient ??= primaryClient;

        return services.AddInkwellAgents(primaryClient, secondaryClient);
    }

    /// <summary>
    /// 注册 Inkwell 默认 Agent 集合（使用多模型服务）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="primaryChatClient">主模型客户端（写作、审核）</param>
    /// <param name="secondaryChatClient">辅助模型客户端（分析、翻译、SEO）</param>
    /// <returns>Agent 注册表</returns>
    public static AgentRegistry AddInkwellAgents(
        this IServiceCollection services,
        IChatClient primaryChatClient,
        IChatClient secondaryChatClient)
    {
        AgentRegistry registry = new();

        // 查找已注册的知识库服务（用于 Writer RAG）
        KnowledgeBaseService? knowledgeBase = null;
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(KnowledgeBaseService)
                && descriptor.ImplementationInstance is KnowledgeBaseService kb)
            {
                knowledgeBase = kb;
                break;
            }
        }

        // 查找已注册的记忆服务（用于 Writer / Coordinator 长期记忆）
        AgentMemoryService? memoryService = null;
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(AgentMemoryService)
                && descriptor.ImplementationInstance is AgentMemoryService ms)
            {
                memoryService = ms;
                break;
            }
        }

        // Primary 模型 Agent（Writer 集成 RAG + 长期记忆）
        List<Microsoft.Agents.AI.AIContextProvider> writerContextProviders = [];
        if (knowledgeBase is not null)
        {
            writerContextProviders.Add(knowledgeBase.CreateSearchProvider());
        }

        if (memoryService is not null)
        {
            writerContextProviders.Add(memoryService.CreateMemoryProvider("writer"));
        }

        registry.Register(InkwellAgents.CreateWriter(primaryChatClient, contextProviders: writerContextProviders));
        registry.Register(InkwellAgents.CreateCritic(primaryChatClient));
        registry.Register(InkwellAgents.CreateImageAnalyst(primaryChatClient));

        // Secondary 模型 Agent
        registry.Register(InkwellAgents.CreateMarketAnalyst(secondaryChatClient));
        registry.Register(InkwellAgents.CreateCompetitorAnalyst(secondaryChatClient));

        AgentRegistration seoRegistration = InkwellAgents.CreateSeoOptimizer(secondaryChatClient);
        registry.Register(seoRegistration);

        registry.Register(InkwellAgents.CreateTranslator(secondaryChatClient, "English"));
        registry.Register(InkwellAgents.CreateTranslator(secondaryChatClient, "Japanese"));

        // Coordinator 将 SEO Agent 作为函数工具（Agent-as-Tool）+ 长期记忆
        List<Microsoft.Agents.AI.AIContextProvider> coordinatorContextProviders = [];
        if (memoryService is not null)
        {
            coordinatorContextProviders.Add(memoryService.CreateMemoryProvider("coordinator"));
        }

        registry.Register(InkwellAgents.CreateCoordinator(secondaryChatClient, seoRegistration.Agent, coordinatorContextProviders));

        services.AddSingleton(registry);

        // 注册工具循环检查点服务（需求 2.16）
        services.AddSingleton<ToolLoopCheckpointService>();

        return registry;
    }

    /// <summary>
    /// 注册 Inkwell 默认 Agent 集合（单模型模式，所有 Agent 共享同一客户端）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册表</returns>
    public static AgentRegistry AddInkwellAgents(this IServiceCollection services, IChatClient chatClient)
    {
        return services.AddInkwellAgents(chatClient, chatClient);
    }
}
