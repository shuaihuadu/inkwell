// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Claims;
using Inkwell.WebApi.Protocols;

namespace Inkwell.WebApi.Tests.Protocols;

/// <summary>
/// 验证动态协议 Agent 的版本路由与 Session 恢复行为。
/// </summary>
[TestClass]
public sealed class RoutingAgentTests
{
    /// <summary>
    /// 验证路由 Agent 只把路由和认证用户标识交给构建服务。
    /// </summary>
    [TestMethod]
    public async Task RunAsync_ValidContext_DelegatesAgentBuildAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        RecordingAgentBuildService buildService = new();
        ServiceCollection services = new();
        services.AddSingleton<IAgentBuildService>(buildService);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues["agentId"] = agentId.ToString();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, ownerUserId.ToString())],
            "test"));
        HttpContextAccessor accessor = new() { HttpContext = httpContext };
        RoutingAgent agent = new(accessor, serviceProvider.GetRequiredService<IServiceScopeFactory>());

        // Act
        AgentSession session = await agent.CreateSessionAsync();
        JsonElement serializedState = await agent.SerializeSessionAsync(session);
        AgentSession restoredSession = await agent.DeserializeSessionAsync(serializedState);
        await agent.RunAsync([new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, "hello")], restoredSession);

        // Assert
        (Guid AgentId, Guid RequestingUserId, bool IsDraft, bool IsTrial) request = buildService.Requests[0];
        Assert.AreEqual(JsonValueKind.Object, serializedState.ValueKind);
        Assert.HasCount(1, buildService.Requests);
        Assert.AreEqual(agentId, request.AgentId);
        Assert.AreEqual(ownerUserId, request.RequestingUserId);
        Assert.IsFalse(request.IsDraft);
        Assert.IsTrue(request.IsTrial);
    }

    /// <summary>
    /// 验证显式 draft 查询参数选择所有者草稿。
    /// </summary>
    [TestMethod]
    public async Task RunAsync_DraftVersionQuery_DelegatesDraftAgentBuildAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        RecordingAgentBuildService buildService = new();
        ServiceCollection services = new();
        services.AddSingleton<IAgentBuildService>(buildService);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues["agentId"] = agentId.ToString();
        httpContext.Request.QueryString = new QueryString("?version=draft");
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, ownerUserId.ToString())],
            "test"));
        HttpContextAccessor accessor = new() { HttpContext = httpContext };
        RoutingAgent agent = new(accessor, serviceProvider.GetRequiredService<IServiceScopeFactory>());

        // Act
        await agent.RunAsync([new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, "hello")]);

        // Assert
        (Guid AgentId, Guid RequestingUserId, bool IsDraft, bool IsTrial) request = buildService.Requests[0];
        Assert.HasCount(1, buildService.Requests);
        Assert.AreEqual(agentId, request.AgentId);
        Assert.AreEqual(ownerUserId, request.RequestingUserId);
        Assert.IsTrue(request.IsDraft);
        Assert.IsTrue(request.IsTrial);
    }

    /// <summary>
    /// 验证流式协议通过专用请求头选择所有者草稿。
    /// </summary>
    [TestMethod]
    public async Task RunStreamingAsync_DraftRunModeHeader_DelegatesDraftAgentBuildAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        RecordingAgentBuildService buildService = new();
        ServiceCollection services = new();
        services.AddSingleton<IAgentBuildService>(buildService);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues["agentId"] = agentId.ToString();
        httpContext.Request.Headers["X-Inkwell-Agent-Run-Mode"] = "draft";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, ownerUserId.ToString())],
            "test"));
        HttpContextAccessor accessor = new() { HttpContext = httpContext };
        RoutingAgent agent = new(accessor, serviceProvider.GetRequiredService<IServiceScopeFactory>());

        // Act
        await foreach (Microsoft.Agents.AI.AgentResponseUpdate _ in agent.RunStreamingAsync(
            [new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, "hello")]))
        {
        }

        // Assert
        (Guid AgentId, Guid RequestingUserId, bool IsDraft, bool IsTrial) request = buildService.Requests[0];
        Assert.HasCount(1, buildService.Requests);
        Assert.AreEqual(agentId, request.AgentId);
        Assert.AreEqual(ownerUserId, request.RequestingUserId);
        Assert.IsTrue(request.IsDraft);
        Assert.IsTrue(request.IsTrial);
    }

    private sealed class RecordingAgentBuildService : IAgentBuildService
    {
        public List<(Guid AgentId, Guid RequestingUserId, bool IsDraft, bool IsTrial)> Requests { get; } = [];

        public ValueTask<AIAgent> BuildDraftAsync(
            Guid agentId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default)
        {
            this.Requests.Add((agentId, requestingUserId, true, false));
            return ValueTask.FromResult<AIAgent>(new StubAgent());
        }

        public ValueTask<AIAgent> BuildDraftTrialAsync(
            Guid agentId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default)
        {
            this.Requests.Add((agentId, requestingUserId, true, true));
            return ValueTask.FromResult<AIAgent>(new StubAgent());
        }

        public ValueTask<AIAgent> BuildPublishedAsync(
            Guid agentId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default)
        {
            this.Requests.Add((agentId, requestingUserId, false, false));
            return ValueTask.FromResult<AIAgent>(new StubAgent());
        }

        public ValueTask<AIAgent> BuildPublishedTrialAsync(
            Guid agentId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default)
        {
            this.Requests.Add((agentId, requestingUserId, false, true));
            return ValueTask.FromResult<AIAgent>(new StubAgent());
        }
    }

    private sealed class StubAgent : AIAgent
    {
        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AgentSession>(new StubAgentSession());

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(JsonSerializer.SerializeToElement(new { state = "stub" }, jsonSerializerOptions));

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AgentSession>(new StubAgentSession());

        protected override Task<Microsoft.Agents.AI.AgentResponse> RunCoreAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            Microsoft.Agents.AI.AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Microsoft.Agents.AI.AgentResponse());

        protected override IAsyncEnumerable<Microsoft.Agents.AI.AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            Microsoft.Agents.AI.AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<Microsoft.Agents.AI.AgentResponseUpdate>();
    }

    private sealed class StubAgentSession : AgentSession;

}
