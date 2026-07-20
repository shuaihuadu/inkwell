// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Runtime.CompilerServices;

namespace Inkwell.Core.Tests.AgentRuntime;

/// <summary>
/// 验证产品会话运行时的固定版本和外部消息历史编排。
/// </summary>
[TestClass]
public sealed class AgentConversationServiceRunTests
{
    /// <summary>
    /// 验证连续两轮流式运行按会话版本构建，并从消息存储恢复历史。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task RunStreamingAsync_TwoRuns_RestoresExternalMessageHistoryAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        DateTimeOffset now = new(2026, 7, 20, 7, 0, 0, TimeSpan.Zero);
        AgentConversation conversation = new()
        {
            Id = conversationId,
            SessionKey = conversationId.ToString("D"),
            AgentId = agentId,
            AgentVersionId = versionId,
            OwnerUserId = ownerUserId,
            LastActivityTime = now,
            CreatedTime = now,
            UpdatedTime = now,
        };
        StubConversationRepository conversations = new(conversation);
        StubMessageRepository messages = new();
        StubPersistenceProvider persistence = new(
            new StubAgentRepository(),
            conversations,
            messages);
        InkwellChatHistoryProvider historyProvider = new(
            persistence,
            new AgentConversationMessageCommitter(persistence, new FixedTimeProvider(now)));
        RecordingBuildService buildService = new(historyProvider);
        AgentConversationService service = new(
            persistence,
            new FixedTimeProvider(now),
            buildService);

        // Act
        List<AgentResponseUpdate> updates = [];
        await foreach (AgentResponseUpdate update in service.RunStreamingAsync(
            ownerUserId,
            agentId,
            conversationId,
            [new ChatMessage(ChatRole.User, "hello")]))
        {
            updates.Add(update);
        }
        await foreach (AgentResponseUpdate update in service.RunStreamingAsync(
            ownerUserId,
            agentId,
            conversationId,
            [new ChatMessage(ChatRole.User, "again")]))
        {
            updates.Add(update);
        }

        // Assert
        Assert.HasCount(2, updates);
        Assert.AreEqual((agentId, versionId, ownerUserId), buildService.PublishedVersionRequest);
        Assert.HasCount(4, messages.AddedMessages);
        Assert.AreEqual(ChatRole.User, messages.AddedMessages[0].Message.Role);
        Assert.AreEqual(ChatRole.Assistant, messages.AddedMessages[1].Message.Role);
        Assert.AreEqual(ChatRole.User, messages.AddedMessages[2].Message.Role);
        Assert.AreEqual(ChatRole.Assistant, messages.AddedMessages[3].Message.Role);
        Assert.HasCount(2, buildService.BuiltAgents);
        Assert.IsTrue(buildService.BuiltAgents.All(agent => agent.RequestMessages.Count == 1));
        Assert.HasCount(3, buildService.BuiltAgents[1].InvocationMessages);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingBuildService(InkwellChatHistoryProvider historyProvider) : IAgentBuildService
    {
        public List<StubAgent> BuiltAgents { get; } = [];

        public (Guid AgentId, Guid VersionId, Guid UserId)? PublishedVersionRequest { get; private set; }

        public ValueTask<AIAgent> BuildPublishedConversationAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default)
        {
            this.PublishedVersionRequest = (agentId, versionId, requestingUserId);
            StubAgent agent = new(historyProvider);
            this.BuiltAgents.Add(agent);
            return ValueTask.FromResult<AIAgent>(agent);
        }

        public ValueTask<AIAgent> BuildDraftAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<AIAgent> BuildDraftTrialAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<AIAgent> BuildPublishedAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<AIAgent> BuildPublishedTrialAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubAgent(InkwellChatHistoryProvider historyProvider) : AIAgent
    {
        public List<ChatMessage> RequestMessages { get; private set; } = [];

        public List<ChatMessage> InvocationMessages { get; private set; } = [];

        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AgentSession>(new StubSession());

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Conversation runs must not serialize MAF sessions.");

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Conversation runs must not deserialize MAF sessions.");

        protected override async Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
        {
            this.RequestMessages = [.. messages];
            ChatHistoryProvider.InvokingContext invokingContext = new(this, session, this.RequestMessages);
            this.InvocationMessages = [.. await historyProvider.InvokingAsync(invokingContext, cancellationToken)];
            ChatMessage response = new(ChatRole.Assistant, "world");
            await historyProvider
                .InvokedAsync(new ChatHistoryProvider.InvokedContext(this, session, this.RequestMessages, [response]), cancellationToken)
                .ConfigureAwait(false);
            return new AgentResponse(response);
        }

        protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            this.RequestMessages = [.. messages];
            ChatHistoryProvider.InvokingContext invokingContext = new(this, session, this.RequestMessages);
            this.InvocationMessages = [.. await historyProvider.InvokingAsync(invokingContext, cancellationToken)];
            await Task.Yield();
            AgentResponseUpdate update = new(ChatRole.Assistant, "world");
            yield return update;
            await historyProvider
                .InvokedAsync(
                    new ChatHistoryProvider.InvokedContext(
                        this,
                        session,
                        this.RequestMessages,
                        [new ChatMessage(ChatRole.Assistant, "world")]),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private sealed class StubSession : AgentSession
    {
    }

    private sealed class StubPersistenceProvider(params object[] repositories) : IPersistenceProvider
    {
        public TRepository GetRepository<TRepository>() where TRepository : notnull => repositories.OfType<TRepository>().Single();

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) => action(ct);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) => action(ct);

        public Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct = default) => action(ct);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) => action(ct);
    }

    private sealed class StubAgentRepository : IAgentRepository
    {
        public Task<AgentDefinition> AddAgent(AgentDefinition agent, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<AgentDefinition> GetAgent(Guid agentId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task UpdateAgent(AgentDefinition agent, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> DeleteAgent(Guid agentId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<PagedResult<AgentDefinition>> ListAgents(Pagination pagination, SortOrder sortOrder, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentDefinition>> FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentDefinition>> FindSharedAgents(Guid excludingOwnerUserId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubConversationRepository(AgentConversation conversation) : IAgentConversationRepository
    {
        private AgentConversation _conversation = conversation;

        public AgentConversation? UpdatedConversation { get; private set; }

        public Task<AgentConversation> GetConversation(Guid conversationId, CancellationToken ct = default) => Task.FromResult(this._conversation);

        public Task<AgentConversation> AddConversation(AgentConversation value, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<AgentConversation> GetConversationBySessionKey(string sessionKey, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<PagedResult<AgentConversationListItem>> ListConversations(Guid agentId, Guid ownerUserId, Pagination pagination, CancellationToken ct = default) => throw new NotSupportedException();

        public Task UpdateConversation(AgentConversation value, CancellationToken ct = default)
        {
            this._conversation = value;
            this.UpdatedConversation = value;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubMessageRepository : IAgentChatMessageRepository
    {
        public List<AgentChatMessage> AddedMessages { get; } = [];

        public Task<IReadOnlyList<AgentChatMessage>> ListAllMessagesByConversation(Guid conversationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentChatMessage>>(this.AddedMessages);

        public Task<PagedResult<AgentChatMessage>> ListMessagesByConversation(Guid conversationId, Pagination pagination, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ChatMessage>> ListHistoryMessagesAsync(Guid conversationId, int? maxMessages = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChatMessage>>(
                this.AddedMessages
                    .Where(message => message.ConversationId == conversationId)
                    .Select(message => message.Message)
                    .TakeLast(maxMessages ?? int.MaxValue)
                    .ToList());

        public Task<IReadOnlyList<AgentChatMessage>> ListMessagesByRun(Guid conversationId, string runId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentChatMessage>>(
                this.AddedMessages.Where(message => message.ConversationId == conversationId && message.RunId == runId).ToList());

        public Task<IReadOnlyList<AgentChatMessage>> AddMessages(IReadOnlyList<AgentChatMessage> messages, CancellationToken ct = default)
        {
            this.AddedMessages.AddRange(messages);
            return Task.FromResult(messages);
        }

        public Task<bool> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<int> DeleteMessagesByConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();
    }

}