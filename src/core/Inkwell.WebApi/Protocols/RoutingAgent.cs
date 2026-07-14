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

    protected override async ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        AgentVersion version = await this.GetVersionAsync(scope.ServiceProvider, null, cancellationToken).ConfigureAwait(false);
        AIAgent agent = await BuildAgentAsync(scope.ServiceProvider, version, cancellationToken).ConfigureAwait(false);
        AgentSession innerSession = await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);

        return new RoutingAgentSession(version.Id, innerSession);
    }

    protected override async ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        RoutingAgentSession routedSession = GetRoutedSession(session);
        using IServiceScope scope = scopeFactory.CreateScope();
        AgentVersion version = await this.GetVersionAsync(scope.ServiceProvider, routedSession.AgentVersionId, cancellationToken).ConfigureAwait(false);
        AIAgent agent = await BuildAgentAsync(scope.ServiceProvider, version, cancellationToken).ConfigureAwait(false);
        JsonElement innerState = await agent.SerializeSessionAsync(routedSession.InnerSession, jsonSerializerOptions, cancellationToken).ConfigureAwait(false);

        return JsonSerializer.SerializeToElement(new RoutingAgentSessionState(version.Id, innerState), jsonSerializerOptions);
    }

    protected override async ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        RoutingAgentSessionState state = serializedState.Deserialize<RoutingAgentSessionState>(jsonSerializerOptions)
            ?? throw new JsonException("The routed agent session state is invalid.");

        using IServiceScope scope = scopeFactory.CreateScope();
        AgentVersion version = await this.GetVersionAsync(scope.ServiceProvider, state.AgentVersionId, cancellationToken).ConfigureAwait(false);
        AIAgent agent = await BuildAgentAsync(scope.ServiceProvider, version, cancellationToken).ConfigureAwait(false);
        AgentSession innerSession = await agent.DeserializeSessionAsync(state.InnerState, jsonSerializerOptions, cancellationToken).ConfigureAwait(false);

        return new RoutingAgentSession(version.Id, innerSession);
    }

    protected override async Task<Microsoft.Agents.AI.AgentResponse> RunCoreAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        Microsoft.Agents.AI.AgentSession? session = null,
        Microsoft.Agents.AI.AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        (AIAgent Agent, RoutingAgentSession Session) runtime =
            await this.ResolveRuntimeAsync(scope.ServiceProvider, session, cancellationToken).ConfigureAwait(false);

        return await runtime.Agent.RunAsync(messages, runtime.Session.InnerSession, options, cancellationToken).ConfigureAwait(false);
    }

    protected override async IAsyncEnumerable<Microsoft.Agents.AI.AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        Microsoft.Agents.AI.AgentSession? session = null,
        Microsoft.Agents.AI.AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        (AIAgent Agent, RoutingAgentSession Session) runtime =
            await this.ResolveRuntimeAsync(scope.ServiceProvider, session, cancellationToken).ConfigureAwait(false);

        await foreach (Microsoft.Agents.AI.AgentResponseUpdate update in runtime.Agent
            .RunStreamingAsync(messages, runtime.Session.InnerSession, options, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private static RoutingAgentSession GetRoutedSession(AgentSession session) =>
        session as RoutingAgentSession
        ?? throw new InvalidOperationException("The session was not created by the routed Inkwell agent.");

    private static async ValueTask<AIAgent> BuildAgentAsync(
        IServiceProvider services,
        AgentVersion version,
        CancellationToken cancellationToken)
    {
        IAgentToolBindingResolver toolResolver = services.GetRequiredService<IAgentToolBindingResolver>();
        IAgentFactory agentFactory = services.GetRequiredService<IAgentFactory>();
        IReadOnlyList<AIFunction> tools = await toolResolver.ResolveAsync(version.Snapshot.ToolBindings, cancellationToken).ConfigureAwait(false);
        AgentBuildOptions buildOptions = new() { Tools = tools };

        return await agentFactory.BuildAsync(version, buildOptions, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<AgentVersion> GetVersionAsync(
        IServiceProvider services,
        Guid? versionId,
        CancellationToken cancellationToken)
    {
        Guid agentId = this.GetRouteAgentId();
        Guid userId = this.GetRequiredUserId();
        IAgentVersionService versionService = services.GetRequiredService<IAgentVersionService>();

        return versionId.HasValue
            ? await versionService.GetPublishedVersionAsync(agentId, versionId.Value, userId, cancellationToken).ConfigureAwait(false)
            : await versionService.GetPublishedVersionAsync(agentId, userId, cancellationToken).ConfigureAwait(false);
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

    private async ValueTask<(AIAgent Agent, RoutingAgentSession Session)> ResolveRuntimeAsync(
        IServiceProvider services,
        AgentSession? session,
        CancellationToken cancellationToken)
    {
        RoutingAgentSession? routedSession = session as RoutingAgentSession;

        if (session is not null && routedSession is null)
        {
            throw new InvalidOperationException("The session was not created by the routed Inkwell agent.");
        }

        AgentVersion version = await this.GetVersionAsync(services, routedSession?.AgentVersionId, cancellationToken).ConfigureAwait(false);
        AIAgent agent = await BuildAgentAsync(services, version, cancellationToken).ConfigureAwait(false);

        if (routedSession is null)
        {
            AgentSession innerSession = await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
            routedSession = new RoutingAgentSession(version.Id, innerSession);
        }

        return (agent, routedSession);
    }
}