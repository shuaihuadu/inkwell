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

        // Primary 模型 Agent
        registry.Register(InkwellAgents.CreateWriter(primaryChatClient));
        registry.Register(InkwellAgents.CreateCritic(primaryChatClient));

        // Secondary 模型 Agent
        registry.Register(InkwellAgents.CreateMarketAnalyst(secondaryChatClient));
        registry.Register(InkwellAgents.CreateSeoOptimizer(secondaryChatClient));
        registry.Register(InkwellAgents.CreateTranslator(secondaryChatClient, "English"));
        registry.Register(InkwellAgents.CreateTranslator(secondaryChatClient, "Japanese"));

        services.AddSingleton(registry);

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
