using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// <see cref="RoutingAgent"/> 的 DI 注册 + ASP.NET Core 端点挂载入口。
/// </summary>
/// <remarks>
/// 唯一目的：把"这行代码需要用到 <c>Microsoft.Agents.AI.AIAgent</c>"这件事收在
/// <c>Inkwell.Core.AgentRuntime</c> 内部——<c>Inkwell.WebApi</c> 只需调用
/// <see cref="UseAgentEndpoints(IEndpointRouteBuilder, string)"/> 这一个扩展方法，
/// 自身代码不需要 <c>using Microsoft.Agents.AI.*</c>（ADR-017 §依赖规则第 3 条精神延续）。
/// </remarks>
public static class AgentEndpointRouteBuilderExtensions
{
    /// <summary>注册 <see cref="RoutingAgent"/> 及其依赖的 <see cref="IHttpContextAccessor"/>。</summary>
    public static IServiceCollection AddRoutingAgent(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddSingleton<RoutingAgent>();

        return services;
    }

    /// <summary>
    /// 挂载 AG-UI 端点，路由形如 <c>{aguiPattern}</c>，须包含
    /// <c>{<see cref="RoutingAgent.AgentIdRouteKey"/>}</c> 路由段（<c>ConversationIdRouteKey</c> 可选，
    /// 缺省表示新对话）。未来新增 OpenAI ChatCompletions/Responses/A2A 协议时，在本方法内追加对应的
    /// <c>Map*</c> 调用即可，<c>Inkwell.WebApi</c> 侧调用方式不变。
    /// </summary>
    /// <param name="endpoints">端点路由构建器。</param>
    /// <param name="aguiPattern">
    /// AG-UI 端点的 URL 模式，例如 <c>"/api/agents/{agentId}/conversations/{conversationId}/agui"</c>。
    /// </param>
    /// <returns>
    /// <c>MapAGUI</c> 返回的端点约定构建器，供调用方链式追加 <c>RequireAuthorization()</c> 等约定
    /// （<c>Inkwell.WebApi</c> 自身不需要因此额外 <c>using Microsoft.Agents.AI.*</c>）。
    /// </returns>
    public static IEndpointConventionBuilder UseAgentEndpoints(this IEndpointRouteBuilder endpoints, string aguiPattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(aguiPattern);

        RoutingAgent routingAgent = endpoints.ServiceProvider.GetRequiredService<RoutingAgent>();

        return endpoints.MapAGUI(aguiPattern, routingAgent);
    }
}
