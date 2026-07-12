// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Protocols;

/// <summary>
/// 注册 MAF 官方协议 Hosting 服务。
/// </summary>
public static class AgentProtocolServiceCollectionExtensions
{
    /// <summary>
    /// 注册 AG-UI、OpenAI Chat Completions、OpenAI Responses 与 OpenAI Conversations Hosting。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>供链式调用的 <paramref name="services"/>。</returns>
    public static IServiceCollection AddAgentProtocols(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddSingleton<RoutingAgent>();
        services.AddAGUI();
        services.AddOpenAIChatCompletions();
        services.AddOpenAIResponses();
        services.AddOpenAIConversations();

        return services;
    }
}