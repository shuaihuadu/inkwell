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

    /// <summary>
    /// 验证绑定产品会话的正式流式运行会提交消息和 Session 检查点。
    /// </summary>
    [TestMethod]
    public async Task RunStreamingAsync_PublishedConversation_CommitsMessagesAndSessionStateAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        RecordingAgentBuildService buildService = new();
        RecordingAgentConversationService conversationService = new(new AgentConversation
        {
            Id = conversationId,
            SessionKey = conversationId.ToString("D"),
            AgentId = agentId,
            AgentVersionId = versionId,
            OwnerUserId = ownerUserId,
            LastActivityTime = DateTimeOffset.UtcNow,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow,
        });
        ServiceCollection services = new();
        services.AddSingleton<IAgentBuildService>(buildService);
        services.AddSingleton<IAgentConversationService>(conversationService);
        services.AddSingleton(TimeProvider.System);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues["agentId"] = agentId.ToString();
        httpContext.Request.Headers["X-Inkwell-Conversation-Id"] = conversationId.ToString();
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
        Assert.HasCount(1, conversationService.CommittedBatches);
        Assert.HasCount(2, conversationService.CommittedBatches[0].Messages);
        Assert.AreEqual(Microsoft.Extensions.AI.ChatRole.User, conversationService.CommittedBatches[0].Messages[0].Role);
        Assert.AreEqual(Microsoft.Extensions.AI.ChatRole.Assistant, conversationService.CommittedBatches[0].Messages[1].Role);
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

        public ValueTask<AIAgent> BuildPublishedConversationAsync(
            Guid agentId,
            Guid versionId,
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

    private sealed class RecordingAgentConversationService(AgentConversation conversation) : IAgentConversationService
    {
        public List<(string ExecutionId, IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> Messages)> CommittedBatches { get; } = [];

        public Task<AgentConversation> CreateConversationAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default) =>
            Task.FromResult(conversation);

        public Task<PagedResult<AgentConversationListItem>> ListConversationsAsync(
            Guid agentId,
            Guid ownerUserId,
            Pagination pagination,
            CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<AgentConversationListItem>([], 0, pagination));

        public Task<PagedResult<AgentChatMessage>> GetMessagesAsync(
            Guid ownerUserId,
            Guid agentId,
            Guid conversationId,
            Pagination pagination,
            CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<AgentChatMessage>([], 0, pagination));

        public Task DeleteMessageAsync(Guid ownerUserId, Guid agentId, Guid conversationId, Guid messageId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task ClearConversationAsync(Guid ownerUserId, Guid agentId, Guid conversationId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task DeleteConversationAsync(Guid ownerUserId, Guid agentId, Guid conversationId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<AgentChatMessageCommitResult> CommitRunMessagesAsync(
            Guid ownerUserId,
            Guid agentId,
            Guid conversationId,
            string executionId,
            IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> messages,
            CancellationToken ct = default)
        {
            this.CommittedBatches.Add((executionId, messages));
            return Task.FromResult(AgentChatMessageCommitResult.Committed);
        }

        public Task<AgentResponse> RunAsync(
            Guid ownerUserId,
            Guid agentId,
            Guid conversationId,
            IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> messages,
            AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentResponse(new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, "world")));

        public async IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
            Guid ownerUserId,
            Guid agentId,
            Guid conversationId,
            IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> messages,
            AgentRunOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string executionId = Guid.CreateVersion7().ToString("D");
            Microsoft.Extensions.AI.ChatMessage assistantMessage = new(Microsoft.Extensions.AI.ChatRole.Assistant, "world");
            _ = await this.CommitRunMessagesAsync(
                ownerUserId,
                agentId,
                conversationId,
                executionId,
                [.. messages, assistantMessage],
                cancellationToken);
            yield return new AgentResponseUpdate(Microsoft.Extensions.AI.ChatRole.Assistant, "world");
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

        protected override async IAsyncEnumerable<Microsoft.Agents.AI.AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            Microsoft.Agents.AI.AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new Microsoft.Agents.AI.AgentResponseUpdate(Microsoft.Extensions.AI.ChatRole.Assistant, "world");
        }
    }

    private sealed class StubAgentSession : AgentSession;

}
