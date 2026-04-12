using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace Inkwell.Agents;

/// <summary>
/// Agent 服务注册扩展方法
/// </summary>
public static class AgentServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Inkwell 默认 Agent 集合
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册表</returns>
    public static AgentRegistry AddInkwellAgents(this IServiceCollection services, IChatClient chatClient)
    {
        AgentRegistry registry = new();

        // 注册预定义 Agent
        registry.Register(InkwellAgents.CreateWriter(chatClient));
        registry.Register(InkwellAgents.CreateCritic(chatClient));
        registry.Register(InkwellAgents.CreateMarketAnalyst(chatClient));
        registry.Register(InkwellAgents.CreateSeoOptimizer(chatClient));
        registry.Register(InkwellAgents.CreateTranslator(chatClient, "English"));
        registry.Register(InkwellAgents.CreateTranslator(chatClient, "Japanese"));

        services.AddSingleton(registry);

        return registry;
    }
}
