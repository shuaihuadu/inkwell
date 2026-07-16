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
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task InvokingAsync_WithAttachedSession_LoadsHistoryBeforeRequestAsync()
    {
        // Arrange
        Guid sessionId = Guid.CreateVersion7();
        ChatMessage historicalMessage = new(ChatRole.User, "history");
        FakeMessageRepository repository = new([historicalMessage]);
        InkwellChatHistoryProvider provider = new(new RecordingPersistenceProvider(repository), 10);
        TestAgent agent = new();
        AgentSession session = await agent.CreateSessionAsync();
        InkwellChatHistoryProvider.AttachSession(session, sessionId);
        ChatMessage requestMessage = new(ChatRole.User, "current");
        ChatHistoryProvider.InvokingContext context = new(agent, session, [requestMessage]);

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
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task InvokedAsync_OnSuccess_AppendsRequestAndResponseInSerializableTransactionAsync()
    {
        // Arrange
        Guid sessionId = Guid.CreateVersion7();
        FakeMessageRepository repository = new([]);
        RecordingPersistenceProvider persistence = new(repository);
        InkwellChatHistoryProvider provider = new(persistence);
        TestAgent agent = new();
        AgentSession session = await agent.CreateSessionAsync();
        InkwellChatHistoryProvider.AttachSession(session, sessionId);
        ChatMessage request = new(ChatRole.User, "question");
        ChatMessage response = new(ChatRole.Assistant, "answer");
        ChatHistoryProvider.InvokedContext context = new(agent, session, [request], [response]);

        // Act
        await provider.InvokedAsync(context);

        // Assert
        Assert.AreEqual(IsolationLevel.Serializable, persistence.LastIsolationLevel);
        Assert.AreEqual(sessionId, repository.AddedMessages[0].ConversationId);
        Assert.HasCount(2, repository.AppendedMessages);
        Assert.AreSame(request, repository.AppendedMessages[0]);
        Assert.AreSame(response, repository.AppendedMessages[1]);
    }

    /// <summary>
    /// 验证失败调用由 MAF 基类拦截，不写入聊天历史。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task InvokedAsync_OnFailure_DoesNotAppendMessagesAsync()
    {
        // Arrange
        FakeMessageRepository repository = new([]);
        RecordingPersistenceProvider persistence = new(repository);
        InkwellChatHistoryProvider provider = new(persistence);
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

    private sealed class RecordingPersistenceProvider(IAgentChatMessageRepository messages) : IPersistenceProvider
    {
        public IsolationLevel? LastIsolationLevel { get; private set; }

        public TRepository GetRepository<TRepository>() where TRepository : notnull =>
            messages is TRepository repository ? repository : throw new NotSupportedException();

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

    private sealed class FakeMessageRepository(IReadOnlyList<ChatMessage> history) : IAgentChatMessageRepository
    {
        public Guid? LastListedSessionId { get; private set; }

        public int? LastMaxMessages { get; private set; }

        public List<ChatMessage> AppendedMessages { get; } = [];

        public List<AgentChatMessage> AddedMessages { get; } = [];

        public Task<PagedResult<AgentChatMessage>> ListMessagesByConversation(Guid conversationId, Pagination pagination, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AgentChatMessage>> ListAllMessagesByConversation(Guid conversationId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AgentChatMessage>> ListMessagesByRun(Guid conversationId, string runId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentChatMessage>> AddMessages(IReadOnlyList<AgentChatMessage> messages, CancellationToken ct = default)
        {
            this.AddedMessages.AddRange(messages);
            this.AppendedMessages.AddRange(messages.Select(message => message.Message));
            return Task.FromResult(messages);
        }

        public Task<IReadOnlyList<ChatMessage>> ListHistoryMessagesAsync(Guid sessionId, int? maxMessages = null, CancellationToken ct = default)
        {
            this.LastListedSessionId = sessionId;
            this.LastMaxMessages = maxMessages;
            return Task.FromResult(history);
        }

        public Task<bool> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<int> DeleteMessagesByConversation(Guid conversationId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
