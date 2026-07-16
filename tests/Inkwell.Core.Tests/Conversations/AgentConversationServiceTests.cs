// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.Conversations;

/// <summary>验证产品会话业务授权和版本锁定。</summary>
[TestClass]
public sealed class AgentConversationServiceTests
{
    /// <summary>验证共享参与用户创建自己的会话并锁定创建时的发布版本。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task CreateConversationAsync_ForSharedParticipant_LocksPublishedVersionAndParticipantOwnerAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        Guid agentOwnerId = Guid.CreateVersion7();
        Guid participantId = Guid.CreateVersion7();
        Guid publishedVersionId = Guid.CreateVersion7();
        DateTimeOffset now = new(2026, 7, 16, 0, 0, 0, TimeSpan.Zero);
        FakeAgentRepository agents = new(CreateAgent(agentId, agentOwnerId, publishedVersionId, isShared: true));
        FakeConversationRepository conversations = new();
        FakePersistenceProvider persistence = new(agents, conversations, new FakeMessageRepository(), new FakeSessionStateRepository());
        AgentConversationService service = CreateService(persistence, now);

        // Act
        AgentConversation result = await service.CreateConversationAsync(agentId, participantId);

        // Assert
        Assert.AreEqual(agentId, result.AgentId);
        Assert.AreEqual(publishedVersionId, result.AgentVersionId);
        Assert.AreEqual(participantId, result.OwnerUserId);
        Assert.AreEqual(result.Id.ToString("D"), result.SessionKey);
        Assert.AreEqual(now, result.LastActivityTime);
        Assert.AreSame(result, conversations.AddedConversation);
        Assert.AreEqual(IsolationLevel.Serializable, persistence.LastIsolationLevel);
    }

    /// <summary>验证同一参与用户不能用错误 Agent 标识访问会话。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task GetMessagesAsync_WithMismatchedAgentId_ThrowsUnauthorizedAccessAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentConversation conversation = new()
        {
            Id = conversationId,
            SessionKey = conversationId.ToString("D"),
            AgentId = agentId,
            AgentVersionId = Guid.CreateVersion7(),
            OwnerUserId = ownerUserId,
            LastActivityTime = now,
            CreatedTime = now,
            UpdatedTime = now,
        };
        FakeConversationRepository conversations = new() { ExistingConversation = conversation };
        AgentConversationService service = CreateService(
            new FakePersistenceProvider(
                new FakeAgentRepository(CreateAgent(agentId, ownerUserId, conversation.AgentVersionId, isShared: false)),
                conversations,
                new FakeMessageRepository(),
                new FakeSessionStateRepository()),
            now);

        // Act
        Task ActAsync() => service.GetMessagesAsync(ownerUserId, Guid.CreateVersion7(), conversationId, Pagination.Default);

        // Assert
        _ = await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(ActAsync);
    }

    /// <summary>验证消息提交由 Service 构造幂等批次并提取标题。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task CommitRunMessagesAsync_AddsBatchAndUpdatesDerivedConversationAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        DateTimeOffset now = new(2026, 7, 16, 3, 0, 0, TimeSpan.Zero);
        AgentConversation conversation = CreateConversation(conversationId, agentId, ownerUserId, now);
        FakeConversationRepository conversations = new() { ExistingConversation = conversation };
        FakeMessageRepository messages = new();
        FakePersistenceProvider persistence = new(
            new FakeAgentRepository(CreateAgent(agentId, ownerUserId, conversation.AgentVersionId, isShared: false)),
            conversations,
            messages,
            new FakeSessionStateRepository());
        AgentConversationService service = CreateService(persistence, now);

        // Act
        AgentChatMessageCommitResult result = await service.CommitRunMessagesAsync(
            ownerUserId,
            agentId,
            conversationId,
            "run-a",
            [new ChatMessage(ChatRole.User, "123456789012345678901234567890more"), new ChatMessage(ChatRole.Assistant, "answer")]);

        // Assert
        Assert.AreEqual(AgentChatMessageCommitResult.Committed, result);
        Assert.HasCount(2, messages.AddedMessages);
        CollectionAssert.AreEqual(new int?[] { 0, 1 }, messages.AddedMessages.Select(message => message.RunMessageIndex).ToArray());
        Assert.AreEqual("123456789012345678901234567890", conversations.UpdatedConversation?.Title);
        Assert.AreEqual("run-a", conversations.UpdatedConversation?.LastCommittedRunId);
        Assert.AreEqual(IsolationLevel.Serializable, persistence.LastIsolationLevel);
    }

    /// <summary>验证 Session State Revision 不连续时由 Service 拒绝且不调用 Repository 写入。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task SaveSessionStateAsync_WithSkippedRevision_ReturnsConflictWithoutWritingAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        DateTimeOffset now = new(2026, 7, 16, 4, 0, 0, TimeSpan.Zero);
        AgentConversation conversation = CreateConversation(conversationId, agentId, ownerUserId, now);
        FakeSessionStateRepository states = new() { ExistingState = CreateState(conversationId, 1, now) };
        FakePersistenceProvider persistence = new(
            new FakeAgentRepository(CreateAgent(agentId, ownerUserId, conversation.AgentVersionId, isShared: false)),
            new FakeConversationRepository { ExistingConversation = conversation },
            new FakeMessageRepository(),
            states);
        AgentConversationService service = CreateService(persistence, now);

        // Act
        AgentSessionStateSaveResult result = await service.SaveSessionStateAsync(
            ownerUserId,
            agentId,
            CreateState(conversationId, 3, now),
            "run-a");

        // Assert
        Assert.AreEqual(AgentSessionStateSaveResult.ConcurrencyConflict, result);
        Assert.IsNull(states.AddedState);
        Assert.IsNull(states.UpdatedState);
        Assert.AreEqual(IsolationLevel.Serializable, persistence.LastIsolationLevel);
    }

    private static AgentConversationService CreateService(FakePersistenceProvider persistence, DateTimeOffset now) =>
        new(
            persistence,
            new FixedTimeProvider(now));

    private static AgentDefinition CreateAgent(Guid id, Guid ownerUserId, Guid publishedVersionId, bool isShared) => new()
    {
        Id = id,
        OwnerUserId = ownerUserId,
        Name = "Conversation agent",
        BuildOptions = new AgentBuildOptions
        {
            ModelOptions = new AgentModelOptions { ModelId = "test-model" },
        },
        IsShared = isShared,
        CurrentPublishedVersionId = publishedVersionId,
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private static AgentConversation CreateConversation(Guid id, Guid agentId, Guid ownerUserId, DateTimeOffset now) => new()
    {
        Id = id,
        SessionKey = id.ToString("D"),
        AgentId = agentId,
        AgentVersionId = Guid.CreateVersion7(),
        OwnerUserId = ownerUserId,
        LastActivityTime = now,
        CreatedTime = now,
        UpdatedTime = now,
    };

    private static AgentSessionState CreateState(Guid conversationId, long revision, DateTimeOffset now) => new()
    {
        ConversationId = conversationId,
        SerializedState = JsonSerializer.SerializeToElement(new { revision }),
        Revision = revision,
        LastRunId = "run-a",
        UpdatedTime = now,
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakePersistenceProvider(params object[] repositories) : IPersistenceProvider
    {
        public IsolationLevel? LastIsolationLevel { get; private set; }

        public TRepository GetRepository<TRepository>() where TRepository : notnull =>
            repositories.OfType<TRepository>().Single();

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

    private sealed class FakeAgentRepository(AgentDefinition agent) : IAgentRepository
    {
        public Task<AgentDefinition> GetAgent(Guid id, CancellationToken ct = default) =>
            id == agent.Id ? Task.FromResult(agent) : throw new KeyNotFoundException();

        public Task<AgentDefinition> AddAgent(AgentDefinition value, CancellationToken ct = default) => throw new NotSupportedException();

        public Task UpdateAgent(AgentDefinition value, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> DeleteAgent(Guid id, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<PagedResult<AgentDefinition>> ListAgents(Pagination pagination, SortOrder sort, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentDefinition>> FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentDefinition>> FindSharedAgents(Guid excludingOwnerUserId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeConversationRepository : IAgentConversationRepository
    {
        public AgentConversation? AddedConversation { get; private set; }

        public AgentConversation? ExistingConversation { get; init; }

        public AgentConversation? UpdatedConversation { get; private set; }

        public Task<AgentConversation> AddConversation(AgentConversation conversation, CancellationToken ct = default)
        {
            this.AddedConversation = conversation;
            return Task.FromResult(conversation);
        }

        public Task<AgentConversation> GetConversation(Guid conversationId, CancellationToken ct = default) =>
            Task.FromResult(this.ExistingConversation is { } conversation && conversation.Id == conversationId
                ? conversation
                : throw new KeyNotFoundException());

        public Task<AgentConversation> GetConversationBySessionKey(string sessionKey, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<PagedResult<AgentConversationListItem>> ListConversations(Guid agentId, Guid ownerUserId, Pagination pagination, CancellationToken ct = default) => throw new NotSupportedException();

        public Task UpdateConversation(AgentConversation conversation, CancellationToken ct = default)
        {
            this.UpdatedConversation = conversation;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeMessageRepository : IAgentChatMessageRepository
    {
        public IReadOnlyList<AgentChatMessage> ExistingRunMessages { get; init; } = [];

        public List<AgentChatMessage> AddedMessages { get; } = [];

        public Task<PagedResult<AgentChatMessage>> ListMessagesByConversation(Guid conversationId, Pagination pagination, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ChatMessage>> ListHistoryMessagesAsync(Guid conversationId, int? maxMessages = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentChatMessage>> ListAllMessagesByConversation(Guid conversationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentChatMessage>>(this.AddedMessages);

        public Task<IReadOnlyList<AgentChatMessage>> ListMessagesByRun(Guid conversationId, string runId, CancellationToken ct = default) =>
            Task.FromResult(this.ExistingRunMessages);

        public Task<IReadOnlyList<AgentChatMessage>> AddMessages(IReadOnlyList<AgentChatMessage> messages, CancellationToken ct = default)
        {
            this.AddedMessages.AddRange(messages);
            return Task.FromResult(messages);
        }

        public Task<bool> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<int> DeleteMessagesByConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeSessionStateRepository : IAgentSessionStateRepository
    {
        public AgentSessionState? ExistingState { get; init; }

        public AgentSessionState? AddedState { get; private set; }

        public AgentSessionState? UpdatedState { get; private set; }

        public Task<AgentSessionState?> GetSessionStateOrDefault(Guid conversationId, CancellationToken ct = default) => Task.FromResult(this.ExistingState);

        public Task AddSessionState(AgentSessionState state, CancellationToken ct = default)
        {
            this.AddedState = state;
            return Task.CompletedTask;
        }

        public Task UpdateSessionState(AgentSessionState state, CancellationToken ct = default)
        {
            this.UpdatedState = state;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteSessionStateByConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
