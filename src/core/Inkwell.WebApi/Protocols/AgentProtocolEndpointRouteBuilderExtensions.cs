// Copyright (c) ShuaiHua Du. All rights reserved.

using AGUI.Abstractions;
using AGUI.Server;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

namespace Inkwell.WebApi.Protocols;

/// <summary>
/// 挂载 MAF 官方协议端点。
/// </summary>
public static class AgentProtocolEndpointRouteBuilderExtensions
{
    /// <summary>
    /// 为动态 Agent 入口挂载 AG-UI、OpenAI Chat Completions、OpenAI Responses 与 OpenAI Conversations API。
    /// </summary>
    /// <param name="endpoints">端点路由生成器。</param>
    /// <param name="authorizationPolicy">应用到所有协议端点的授权策略；为 null 时不附加策略。</param>
    /// <returns>供链式调用的 <paramref name="endpoints"/>。</returns>
    public static IEndpointRouteBuilder MapAgentProtocols(
        this IEndpointRouteBuilder endpoints,
        string? authorizationPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RoutingAgent agent = endpoints.ServiceProvider.GetRequiredService<RoutingAgent>();

        IEndpointConventionBuilder agui = endpoints
            .MapAGUIServer("/agent/{agentId}", agent)
            .WithMetadata(CreateStreamOptions());
        IEndpointConventionBuilder chatCompletions = endpoints.MapOpenAIChatCompletions(
            agent,
            "/agent/{agentId}/v1/chat/completions");
        IEndpointConventionBuilder responses = endpoints.MapOpenAIResponses(
            agent,
            "/agent/{agentId}/v1/responses");
        IEndpointConventionBuilder conversations = endpoints.MapOpenAIConversations();

        if (!string.IsNullOrWhiteSpace(authorizationPolicy))
        {
            agui.RequireAuthorization(authorizationPolicy);
            chatCompletions.RequireAuthorization(authorizationPolicy);
            responses.RequireAuthorization(authorizationPolicy);
            conversations.RequireAuthorization(authorizationPolicy);
        }

        return endpoints;
    }

    private static AGUIStreamOptions CreateStreamOptions() => new AGUIStreamOptions()
        .MapContent(MapUsageContent);

#pragma warning disable MEAI001
    private static IEnumerable<BaseEvent> MapUsageContent(AIContent content)
    {
        if (content is not UsageContent usageContent)
        {
            return [];
        }

        return
        [
            new CustomEvent
            {
                Name = "inkwell.token_usage",
                Value = JsonSerializer.SerializeToElement(new
                {
                    inputTokenCount = usageContent.Details.InputTokenCount,
                    outputTokenCount = usageContent.Details.OutputTokenCount,
                    totalTokenCount = usageContent.Details.TotalTokenCount,
                    cachedInputTokenCount = usageContent.Details.CachedInputTokenCount,
                    reasoningTokenCount = usageContent.Details.ReasoningTokenCount,
                    additionalCounts = usageContent.Details.AdditionalCounts,
                }),
            },
        ];
    }
#pragma warning restore MEAI001
}