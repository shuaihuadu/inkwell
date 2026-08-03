// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace Inkwell.Core.Tests.AgentRuntime;

/// <summary>验证 Agent Session 检查点的持久化、还原与容错行为。</summary>
[TestClass]
public sealed class InkwellAgentSessionStateStoreTests
{
    private static readonly Guid conversationId = Guid.ParseExact("b6a0f6d64f3f4f0f9a2f1a0c5d7e8f90", "N");

    /// <summary>验证状态行不存在时新建空 Session 而不是抛出。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task GetSessionAsync_WithMissingState_CreatesNewSessionAsync()
    {
        // Arrange
        FakeSessionStateRepository sessionStates = new();
        InkwellAgentSessionStateStore store = CreateStore(sessionStates);
        TestAgent agent = new();

        // Act
        AgentSession session = await store.GetSessionAsync(agent, conversationId);

        // Assert
        Assert.IsNotNull(session);
        Assert.AreEqual(1, agent.CreateSessionCount);
        Assert.AreEqual(0, agent.DeserializeSessionCount);
    }

    /// <summary>验证状态为空 JSON 时不走反序列化，直接新建空 Session。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task GetSessionAsync_WithEmptyState_CreatesNewSessionAsync()
    {
        // Arrange
        FakeSessionStateRepository sessionStates = new();
        await sessionStates.AddSessionState(CreateState(AgentSessionState.Empty));
        InkwellAgentSessionStateStore store = CreateStore(sessionStates);
        TestAgent agent = new();

        // Act
        _ = await store.GetSessionAsync(agent, conversationId);

        // Assert
        Assert.AreEqual(1, agent.CreateSessionCount);
        Assert.AreEqual(0, agent.DeserializeSessionCount);
    }

    /// <summary>验证已保存的状态能被还原回同一个 Session 状态包。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task GetSessionAsync_WithSavedState_RestoresStateBagAsync()
    {
        // Arrange
        FakeSessionStateRepository sessionStates = new();
        InkwellAgentSessionStateStore store = CreateStore(sessionStates);
        TestAgent agent = new();
        AgentSession original = await agent.CreateSessionAsync();
        original.StateBag.SetValue("checkpoint", "round-1");
        await store.SaveSessionAsync(agent, conversationId, original);

        // Act
        AgentSession restored = await store.GetSessionAsync(agent, conversationId);

        // Assert
        Assert.AreEqual(1, agent.DeserializeSessionCount);
        Assert.IsTrue(restored.StateBag.TryGetValue("checkpoint", out string? checkpoint));
        Assert.AreEqual("round-1", checkpoint);
    }

    /// <summary>验证状态内容不可反序列化时丢弃并新建空 Session，不向调用方抛出。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task GetSessionAsync_WithUnreadableState_FallsBackToNewSessionAsync()
    {
        // Arrange
        FakeSessionStateRepository sessionStates = new();
        await sessionStates.AddSessionState(CreateState("{ not-json"));
        InkwellAgentSessionStateStore store = CreateStore(sessionStates);
        TestAgent agent = new();

        // Act
        AgentSession session = await store.GetSessionAsync(agent, conversationId);

        // Assert
        Assert.IsNotNull(session);
        Assert.AreEqual(1, agent.CreateSessionCount);
    }

    /// <summary>验证保存时按产品会话标识覆盖写入并刷新更新时间。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task SaveSessionAsync_WithExistingState_OverwritesContentAndTimestampAsync()
    {
        // Arrange
        DateTimeOffset now = new(2026, 7, 26, 0, 0, 0, TimeSpan.Zero);
        FakeSessionStateRepository sessionStates = new();
        await sessionStates.AddSessionState(CreateState(AgentSessionState.Empty));
        InkwellAgentSessionStateStore store = CreateStore(sessionStates, now);
        TestAgent agent = new();
        AgentSession session = await agent.CreateSessionAsync();
        session.StateBag.SetValue("checkpoint", "round-1");

        // Act
        await store.SaveSessionAsync(agent, conversationId, session);

        // Assert
        Assert.AreEqual(1, agent.SerializeSessionCount);
        Assert.AreNotEqual(AgentSessionState.Empty, sessionStates.States[conversationId].SessionState);
        Assert.AreEqual(now, sessionStates.States[conversationId].UpdatedTime);
    }

    /// <summary>验证状态行缺失时保存会新建状态行，调用方无需预先建行。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task SaveSessionAsync_WithMissingState_CreatesStateRowAsync()
    {
        // Arrange
        DateTimeOffset now = new(2026, 7, 26, 0, 0, 0, TimeSpan.Zero);
        FakeSessionStateRepository sessionStates = new();
        InkwellAgentSessionStateStore store = CreateStore(sessionStates, now);
        TestAgent agent = new();
        AgentSession session = await agent.CreateSessionAsync();
        session.StateBag.SetValue("checkpoint", "round-1");

        // Act
        await store.SaveSessionAsync(agent, conversationId, session);

        // Assert
        Assert.AreEqual(1, sessionStates.States.Count);
        Assert.AreEqual(conversationId, sessionStates.States[conversationId].ConversationId);
        Assert.AreNotEqual(AgentSessionState.Empty, sessionStates.States[conversationId].SessionState);
        Assert.AreEqual(now, sessionStates.States[conversationId].CreatedTime);
    }

    /// <summary>验证删除会移除状态行，下次获取时回落为新建 Session。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task DeleteSessionStateAsync_WithExistingState_RemovesStateRowAsync()
    {
        // Arrange
        FakeSessionStateRepository sessionStates = new();
        await sessionStates.AddSessionState(CreateState("""{"checkpoint":"round-1"}"""));
        InkwellAgentSessionStateStore store = CreateStore(sessionStates);
        TestAgent agent = new();

        // Act
        await store.DeleteSessionStateAsync(conversationId);

        // Assert
        Assert.IsEmpty(sessionStates.States);
        _ = await store.GetSessionAsync(agent, conversationId);
        Assert.AreEqual(1, agent.CreateSessionCount);
    }

    private static InkwellAgentSessionStateStore CreateStore(
        FakeSessionStateRepository sessionStates,
        DateTimeOffset? now = null) =>
        new(
            new FakePersistenceProvider(sessionStates),
            new FixedTimeProvider(now ?? new DateTimeOffset(2026, 7, 26, 0, 0, 0, TimeSpan.Zero)),
            NullLogger<InkwellAgentSessionStateStore>.Instance);

    private static AgentSessionState CreateState(string sessionState) => new()
    {
        Id = Guid.CreateVersion7(),
        ConversationId = conversationId,
        SessionState = sessionState,
        CreatedTime = DateTimeOffset.UnixEpoch,
        UpdatedTime = DateTimeOffset.UnixEpoch,
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeSessionStateRepository : IAgentSessionStateRepository
    {
        private readonly Dictionary<Guid, AgentSessionState> _states = [];

        public Dictionary<Guid, AgentSessionState> States => this._states;

        public Task<AgentSessionState> AddSessionState(AgentSessionState sessionState, CancellationToken ct = default)
        {
            this._states[sessionState.ConversationId] = sessionState;

            return Task.FromResult(sessionState);
        }

        public Task<AgentSessionState?> FindSessionStateByConversation(Guid conversationId, CancellationToken ct = default) =>
            Task.FromResult(this._states.TryGetValue(conversationId, out AgentSessionState? state) ? state : null);

        public Task<bool> UpdateSessionState(Guid conversationId, string sessionState, DateTimeOffset updatedTime, CancellationToken ct = default)
        {
            if (!this._states.TryGetValue(conversationId, out AgentSessionState? existing))
            {
                return Task.FromResult(false);
            }

            this._states[conversationId] = existing with { SessionState = sessionState, UpdatedTime = updatedTime };

            return Task.FromResult(true);
        }

        public Task<bool> DeleteSessionState(Guid conversationId, CancellationToken ct = default) =>
            Task.FromResult(this._states.Remove(conversationId));
    }

    private sealed class FakePersistenceProvider(params object[] repositories) : IPersistenceProvider
    {
        public TRepository GetRepository<TRepository>() where TRepository : notnull =>
            repositories.OfType<TRepository>().Single();

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            action(ct);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            action(ct);

        public Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            action(ct);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            action(ct);
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

        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        {
            this.CreateSessionCount++;

            return ValueTask.FromResult<AgentSession>(new TestSession());
        }

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            this.SerializeSessionCount++;

            return ValueTask.FromResult(session.StateBag.Serialize());
        }

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            this.DeserializeSessionCount++;

            return ValueTask.FromResult<AgentSession>(new TestSession(AgentSessionStateBag.Deserialize(serializedState)));
        }

        protected override Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask.ConfigureAwait(false);

            yield break;
        }
    }
}
