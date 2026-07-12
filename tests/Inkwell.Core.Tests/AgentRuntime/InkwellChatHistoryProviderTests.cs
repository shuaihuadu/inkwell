// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Runtime.CompilerServices;

namespace Inkwell.Core.Tests.AgentRuntime;

/// <summary>
/// 验证 Inkwell 持久化聊天历史与 MAF Provider 生命周期的衔接。
/// </summary>
[TestClass]
public sealed class InkwellChatHistoryProviderTests
{
    /// <summary>
    /// 验证 Provider 从 StateBag 路由业务 Session，并把历史置于当前请求之前。
    /// </summary>
    [TestMethod]
    public async Task InvokingAsync_WithAttachedSession_LoadsHistoryBeforeRequestAsync()
    {
        // Arrange
        Guid sessionId = Guid.CreateVersion7();
        ChatMessage historicalMessage = new(ChatRole.User, "history");
        FakeMessageRepository repository = new([historicalMessage]);
        InkwellChatHistoryProvider provider = new(repository, new RecordingPersistenceProvider(), 10);
        TestAgent agent = new();
        AgentSession session = await agent.CreateSessionAsync();
        InkwellChatHistoryProvider.AttachSession(session, sessionId);
        ChatMessage requestMessage = new(ChatRole.User, "current");
#pragma warning disable MAAI001
        ChatHistoryProvider.InvokingContext context = new(agent, session, [requestMessage]);
#pragma warning restore MAAI001

        // Act
        List<ChatMessage> messages = [.. await provider.InvokingAsync(context)];

        // Assert
        Assert.HasCount(2, messages);
        Assert.AreEqual("history", messages[0].Text);
        Assert.AreEqual("current", messages[1].Text);
        Assert.AreEqual(sessionId, repository.LastListedSessionId);
        Assert.AreEqual(10, repository.LastMaxMessages);
    }

    /// <summary>
    /// 验证成功调用把请求和响应作为一个可串行化事务批次保存。
    /// </summary>
    [TestMethod]
    public async Task InvokedAsync_OnSuccess_AppendsRequestAndResponseInSerializableTransactionAsync()
    {
        // Arrange
        Guid sessionId = Guid.CreateVersion7();
        FakeMessageRepository repository = new([]);
        RecordingPersistenceProvider persistence = new();
        InkwellChatHistoryProvider provider = new(repository, persistence);
        TestAgent agent = new();
        AgentSession session = await agent.CreateSessionAsync();
        InkwellChatHistoryProvider.AttachSession(session, sessionId);
        ChatMessage request = new(ChatRole.User, "question");
        ChatMessage response = new(ChatRole.Assistant, "answer");
#pragma warning disable MAAI001
        ChatHistoryProvider.InvokedContext context = new(agent, session, [request], [response]);
#pragma warning restore MAAI001

        // Act
        await provider.InvokedAsync(context);

        // Assert
        Assert.AreEqual(IsolationLevel.Serializable, persistence.LastIsolationLevel);
        Assert.AreEqual(sessionId, repository.LastAppendedSessionId);
        Assert.HasCount(2, repository.AppendedMessages);
        Assert.AreSame(request, repository.AppendedMessages[0]);
        Assert.AreSame(response, repository.AppendedMessages[1]);
    }

    /// <summary>
    /// 验证失败调用由 MAF 基类拦截，不写入聊天历史。
    /// </summary>
    [TestMethod]
    public async Task InvokedAsync_OnFailure_DoesNotAppendMessagesAsync()
    {
        // Arrange
        FakeMessageRepository repository = new([]);
        RecordingPersistenceProvider persistence = new();
        InkwellChatHistoryProvider provider = new(repository, persistence);
        TestAgent agent = new();
        AgentSession session = await agent.CreateSessionAsync();
        InkwellChatHistoryProvider.AttachSession(session, Guid.CreateVersion7());
        ChatHistoryProvider.InvokedContext context = new(agent, session, [new ChatMessage(ChatRole.User, "question")], new InvalidOperationException("failed"));

        // Act
        await provider.InvokedAsync(context);

        // Assert
        Assert.HasCount(0, repository.AppendedMessages);
        Assert.IsNull(persistence.LastIsolationLevel);
    }

    /// <summary>
    /// 验证业务 Session 状态只能通过对应 Agent 创建、捕获并恢复。
    /// </summary>
    [TestMethod]
    public async Task AgentSessionRuntime_CreateCaptureAndRestore_PreservesStateAsync()
    {
        // Arrange
        Guid sessionId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        Guid agentVersionId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentSessionDefinition definition = new()
        {
            Id = sessionId,
            AgentId = agentId,
            AgentVersionId = agentVersionId,
            OwnerUserId = ownerUserId,
            CreatedTime = now,
            UpdatedTime = now,
        };
        TestAgent creatingAgent = new();

        // Act
        AgentSession createdSession = await AgentSessionRuntime.OpenAsync(creatingAgent, definition);
        createdSession.StateBag.SetValue("test-state", "preserved");
        AgentSessionDefinition capturedDefinition = await AgentSessionRuntime.CaptureAsync(creatingAgent, createdSession, definition);
        TestAgent restoringAgent = new();
        AgentSession restoredSession = await AgentSessionRuntime.OpenAsync(restoringAgent, capturedDefinition);

        // Assert
        Assert.AreEqual(1, creatingAgent.CreateSessionCount);
        Assert.AreEqual(1, creatingAgent.SerializeSessionCount);
        Assert.AreEqual(1, restoringAgent.DeserializeSessionCount);
        Assert.AreEqual(sessionId.ToString(), restoredSession.StateBag.GetValue<string>(InkwellChatHistoryProvider.SessionIdStateKey));
        Assert.AreEqual("preserved", restoredSession.StateBag.GetValue<string>("test-state"));
    }

    private sealed class TestSession : AgentSession
    {
        public TestSession()
        {
        }

        public TestSession(AgentSessionStateBag stateBag)
            : base(stateBag)
        {
        }
    }

    private sealed class TestAgent : AIAgent
    {
        public int CreateSessionCount { get; private set; }

        public int SerializeSessionCount { get; private set; }

        public int DeserializeSessionCount { get; private set; }

        protected override ValueTask<Microsoft.Agents.AI.AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        {
            this.CreateSessionCount++;
            return ValueTask.FromResult<Microsoft.Agents.AI.AgentSession>(new TestSession());
        }

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(Microsoft.Agents.AI.AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        {
            this.SerializeSessionCount++;
            return ValueTask.FromResult(session.StateBag.Serialize());
        }

        protected override ValueTask<Microsoft.Agents.AI.AgentSession> DeserializeSessionCoreAsync(JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        {
            this.DeserializeSessionCount++;
            return ValueTask.FromResult<Microsoft.Agents.AI.AgentSession>(new TestSession(AgentSessionStateBag.Deserialize(serializedState)));
        }

        protected override Task<Microsoft.Agents.AI.AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            Microsoft.Agents.AI.AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        protected override async IAsyncEnumerable<Microsoft.Agents.AI.AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            Microsoft.Agents.AI.AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }
    }

    private sealed class RecordingPersistenceProvider : IPersistenceProvider
    {
        public IsolationLevel? LastIsolationLevel { get; private set; }

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            await action(ct).ConfigureAwait(false);

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            await action(ct).ConfigureAwait(false);

        public async Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct = default)
        {
            this.LastIsolationLevel = isolationLevel;
            await action(ct).ConfigureAwait(false);
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
        {
            this.LastIsolationLevel = isolationLevel;
            return await action(ct).ConfigureAwait(false);
        }
    }

    private sealed class FakeMessageRepository(IReadOnlyList<ChatMessage> history) : IAgentSessionMessageRepository
    {
        public Guid? LastListedSessionId { get; private set; }

        public int? LastMaxMessages { get; private set; }

        public Guid? LastAppendedSessionId { get; private set; }

        public List<ChatMessage> AppendedMessages { get; } = [];

        public Task<AgentChatMessage> AddMessage(AgentChatMessage message, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<PagedResult<AgentChatMessage>> ListMessagesBySession(Guid sessionId, Pagination pagination, SortOrder sort, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ChatMessage>> ListHistoryMessagesAsync(Guid sessionId, int? maxMessages = null, CancellationToken ct = default)
        {
            this.LastListedSessionId = sessionId;
            this.LastMaxMessages = maxMessages;
            return Task.FromResult(history);
        }

        public Task<IReadOnlyList<AgentChatMessage>> AppendMessagesAsync(Guid sessionId, IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
        {
            this.LastAppendedSessionId = sessionId;
            this.AppendedMessages.AddRange(messages);
            return Task.FromResult<IReadOnlyList<AgentChatMessage>>([]);
        }

        public Task<bool> DeleteMessage(Guid sessionId, Guid messageId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> DeleteMessagesBySession(Guid sessionId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
