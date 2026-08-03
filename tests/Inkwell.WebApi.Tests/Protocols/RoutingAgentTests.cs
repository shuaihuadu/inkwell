// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Diagnostics;
using System.Security.Claims;
using AGUI.Abstractions;
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
    /// 验证绑定产品会话的正式流式运行会把路由与身份上下文透传给会话服务。
    /// </summary>
    [TestMethod]
    public async Task RunStreamingAsync_PublishedConversation_ForwardsRoutingContextAsync()
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
        Assert.HasCount(1, conversationService.StreamingRuns);
        Assert.AreEqual(ownerUserId, conversationService.StreamingRuns[0].OwnerUserId);
        Assert.AreEqual(agentId, conversationService.StreamingRuns[0].AgentId);
        Assert.AreEqual(conversationId, conversationService.StreamingRuns[0].ConversationId);
        Assert.HasCount(1, conversationService.StreamingRuns[0].Messages);
        Assert.AreEqual(Microsoft.Extensions.AI.ChatRole.User, conversationService.StreamingRuns[0].Messages[0].Role);
    }

    /// <summary>
    /// 验证未携带请求头时，会话标识取自 AG-UI 协议的 threadId。
    /// </summary>
    [TestMethod]
    public async Task RunStreamingAsync_AGUIThreadId_ResolvesConversationAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        RecordingAgentBuildService buildService = new();
        RecordingAgentConversationService conversationService = new(new AgentConversation
        {
            Id = conversationId,
            AgentId = agentId,
            AgentVersionId = Guid.CreateVersion7(),
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
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, ownerUserId.ToString())],
            "test"));
        HttpContextAccessor accessor = new() { HttpContext = httpContext };
        RoutingAgent agent = new(accessor, serviceProvider.GetRequiredService<IServiceScopeFactory>());
        Microsoft.Extensions.AI.ChatOptions chatOptions = new()
        {
            AdditionalProperties = new Microsoft.Extensions.AI.AdditionalPropertiesDictionary
            {
                // AGUI.Server.AGUIConstants.RunAgentInputKey 为 internal，此处使用其字面值。
                ["agui_input"] = new RunAgentInput
                {
                    ThreadId = conversationId.ToString(),
                    RunId = Guid.CreateVersion7().ToString(),
                },
            },
        };

        // Act
        await foreach (Microsoft.Agents.AI.AgentResponseUpdate _ in agent.RunStreamingAsync(
            [new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, "hello")],
            options: new ChatClientAgentRunOptions(chatOptions)))
        {
        }

        // Assert
        Assert.HasCount(1, conversationService.StreamingRuns);
        Assert.AreEqual(conversationId, conversationService.StreamingRuns[0].ConversationId);
        Assert.IsEmpty(buildService.Requests);
    }

    /// <summary>
    /// 验证 AG-UI threadId 无法解析为会话标识时拒绝请求，而不是降级为不落库的试运行。
    /// </summary>
    [TestMethod]
    public async Task RunStreamingAsync_InvalidAGUIThreadId_RejectsRequestAsync()
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
        Microsoft.Extensions.AI.ChatOptions chatOptions = new()
        {
            AdditionalProperties = new Microsoft.Extensions.AI.AdditionalPropertiesDictionary
            {
                // 社区 SDK 允许非 UUID 的 threadId，例如 Kotlin SDK 生成的 "id_1785283200000"。
                ["agui_input"] = new RunAgentInput
                {
                    ThreadId = "id_1785283200000",
                    RunId = Guid.CreateVersion7().ToString(),
                },
            },
        };

        // Act
        ArgumentException exception = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await foreach (Microsoft.Agents.AI.AgentResponseUpdate _ in agent.RunStreamingAsync(
                [new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, "hello")],
                options: new ChatClientAgentRunOptions(chatOptions)))
            {
            }
        });

        // Assert
        Assert.Contains("threadId", exception.Message);
        Assert.IsEmpty(buildService.Requests);
    }

    /// <summary>
    /// 验证会话标识请求头格式非法时拒绝请求，而不是降级为不落库的试运行。
    /// </summary>
    [TestMethod]
    public async Task RunAsync_InvalidConversationIdHeader_RejectsRequestAsync()
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
        httpContext.Request.Headers["X-Inkwell-Conversation-Id"] = "not-a-guid";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, ownerUserId.ToString())],
            "test"));
        HttpContextAccessor accessor = new() { HttpContext = httpContext };
        RoutingAgent agent = new(accessor, serviceProvider.GetRequiredService<IServiceScopeFactory>());

        // Act
        ArgumentException exception = await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            agent.RunAsync([new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, "hello")]));

        // Assert
        Assert.Contains("X-Inkwell-Conversation-Id", exception.Message);
        Assert.IsEmpty(buildService.Requests);
    }

    /// <summary>
    /// 验证客户端 AG-UI runId 以截断后的 trace 标签记录，供前后端日志对账。
    /// </summary>
    [TestMethod]
    public async Task RunStreamingAsync_AGUIRunId_RecordsTruncatedProtocolTagsAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        string oversizedRunId = new('r', 80);
        string parentRunId = "parent-run";
        RecordingAgentBuildService buildService = new();
        RecordingAgentConversationService conversationService = new(new AgentConversation
        {
            Id = conversationId,
            AgentId = agentId,
            AgentVersionId = Guid.CreateVersion7(),
            OwnerUserId = ownerUserId,
            LastActivityTime = DateTimeOffset.UtcNow,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow,
        });
        ServiceCollection services = new();
        services.AddSingleton<IAgentBuildService>(buildService);
        services.AddSingleton<IAgentConversationService>(conversationService);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues["agentId"] = agentId.ToString();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, ownerUserId.ToString())],
            "test"));
        HttpContextAccessor accessor = new() { HttpContext = httpContext };
        RoutingAgent agent = new(accessor, serviceProvider.GetRequiredService<IServiceScopeFactory>());
        Microsoft.Extensions.AI.ChatOptions chatOptions = new()
        {
            AdditionalProperties = new Microsoft.Extensions.AI.AdditionalPropertiesDictionary
            {
                ["agui_input"] = new RunAgentInput
                {
                    ThreadId = conversationId.ToString(),
                    RunId = oversizedRunId,
                    ParentRunId = parentRunId,
                },
            },
        };
        using ActivitySource activitySource = new(nameof(RoutingAgentTests));
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == nameof(RoutingAgentTests),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        using Activity? activity = activitySource.StartActivity("run");

        // Act
        await foreach (Microsoft.Agents.AI.AgentResponseUpdate _ in agent.RunStreamingAsync(
            [new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, "hello")],
            options: new ChatClientAgentRunOptions(chatOptions)))
        {
        }

        // Assert
        Assert.IsNotNull(activity);
        Assert.AreEqual(oversizedRunId[..64], activity.GetTagItem("agui.protocol_run_id"));
        Assert.AreEqual(parentRunId, activity.GetTagItem("agui.protocol_parent_run_id"));
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
        public List<(Guid OwnerUserId, Guid AgentId, Guid ConversationId, IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> Messages)> StreamingRuns { get; } = [];

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
            this.StreamingRuns.Add((ownerUserId, agentId, conversationId, messages));
            await Task.CompletedTask;
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
