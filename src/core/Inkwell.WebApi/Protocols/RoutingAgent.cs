// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell.WebApi.Protocols;

internal sealed class RoutingAgent(
    IHttpContextAccessor httpContextAccessor,
    IServiceScopeFactory scopeFactory) : AIAgent
{
    public override string? Name => "inkwell-routed-agent";

    public override string? Description => "Resolves the published Inkwell agent version selected by the current route.";

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<AgentSession>(new StatelessAgentSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(JsonSerializer.SerializeToElement(new { }, jsonSerializerOptions));

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<AgentSession>(new StatelessAgentSession());

    protected override async Task<Microsoft.Agents.AI.AgentResponse> RunCoreAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        Microsoft.Agents.AI.AgentSession? session = null,
        Microsoft.Agents.AI.AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // RoutingAgent 由 Hosting 以 Singleton 持有；每次 Run 单独创建 Scope，避免跨请求复用构建服务及其持久化依赖。
        using IServiceScope scope = scopeFactory.CreateScope();
        IAgentBuildService buildService = scope.ServiceProvider.GetRequiredService<IAgentBuildService>();
        AIAgent agent = await buildService
            .BuildPublishedAsync(this.GetRouteAgentId(), this.GetRequiredUserId(), cancellationToken)
            .ConfigureAwait(false);

        return await agent.RunAsync(messages, null, options, cancellationToken).ConfigureAwait(false);
    }

    protected override async IAsyncEnumerable<Microsoft.Agents.AI.AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        Microsoft.Agents.AI.AgentSession? session = null,
        Microsoft.Agents.AI.AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Scope 必须覆盖整个流式枚举，确保构建服务及其持久化依赖在响应结束前保持有效。
        using IServiceScope scope = scopeFactory.CreateScope();
        IAgentBuildService buildService = scope.ServiceProvider.GetRequiredService<IAgentBuildService>();
        AIAgent agent = await buildService
            .BuildPublishedAsync(this.GetRouteAgentId(), this.GetRequiredUserId(), cancellationToken)
            .ConfigureAwait(false);

        await foreach (Microsoft.Agents.AI.AgentResponseUpdate update in agent
            .RunStreamingAsync(messages, null, options, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private Guid GetRouteAgentId()
    {
        string? routeValue = httpContextAccessor.HttpContext?.Request.RouteValues["agentId"]?.ToString();

        return Guid.TryParse(routeValue, out Guid agentId)
            ? agentId
            : throw new InvalidOperationException("The route does not contain a valid agentId.");
    }

    private Guid GetRequiredUserId()
    {
        string? claimValue = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claimValue, out Guid userId)
            ? userId
            : throw new UnauthorizedAccessException("The authenticated user identifier is missing or invalid.");
    }

    private sealed class StatelessAgentSession : AgentSession;
}
