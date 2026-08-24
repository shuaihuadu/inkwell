// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;

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
            AgentId = agentId,
            AgentVersionId = versionId,
            OwnerUserId = ownerUserId,
            LastActivityTime = now,
            CreatedTime = now,
            UpdatedTime = now,
        };
        StubConversationRepository conversations = new(conversation);
        StubMessageRepository messages = new();
        StubSessionStateRepository sessionStates = new();
        await sessionStates.AddSessionState(new AgentSessionState
        {
            Id = Guid.CreateVersion7(),
            ConversationId = conversationId,
            SessionState = AgentSessionState.Empty,
            CreatedTime = now,
            UpdatedTime = now,
        });
        StubPersistenceProvider persistence = new(
            new StubAgentRepository(),
            conversations,
            messages,
            sessionStates);
        InkwellChatHistoryProvider historyProvider = new(
            persistence,
            new FixedTimeProvider(now));
        RecordingBuildService buildService = new(historyProvider);
        AgentConversationService service = new(
            persistence,
            new FixedTimeProvider(now),
            buildService,
            new InkwellAgentSessionStateStore(
                persistence,
                new FixedTimeProvider(now),
                NullLogger<InkwellAgentSessionStateStore>.Instance),
            NullLogger<AgentConversationService>.Instance);

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

    /// <summary>验证流式 Tool loop 用量在自然完成后聚合到最后一条 assistant 消息。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task RunStreamingAsync_WithMultipleUsageContents_PersistsAggregateOnAssistantMessageAsync()
    {
        // Arrange
        RunTestContext context = await CreateRunTestContextAsync();
        context.BuildService.StreamingUsages =
        [
            new UsageDetails { InputTokenCount = 2, OutputTokenCount = 3, TotalTokenCount = 5 },
            new UsageDetails { InputTokenCount = 7, OutputTokenCount = 11, TotalTokenCount = 18 },
        ];

        // Act
        await foreach (AgentResponseUpdate _ in context.Service.RunStreamingAsync(
            context.OwnerUserId,
            context.AgentId,
            context.ConversationId,
            [new ChatMessage(ChatRole.User, "hello")]))
        {
        }

        // Assert
        Assert.HasCount(2, context.Messages.AddedMessages);
        Assert.IsNull(context.Messages.AddedMessages[0].Usage);
        UsageDetails persisted = context.Messages.AddedMessages[1].Usage!;
        Assert.AreEqual(9, persisted.InputTokenCount);
        Assert.AreEqual(14, persisted.OutputTokenCount);
        Assert.AreEqual(23, persisted.TotalTokenCount);
        CollectionAssert.AreEqual(
            new[] { "session-saved", "usage-updated" },
            context.PersistenceOperations.ToArray());
    }

    /// <summary>验证消费方提前停止流式枚举时不保存 Session 或部分 Token 用量。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task RunStreamingAsync_WhenConsumerStopsAfterUsage_DoesNotPersistPartialRunAsync()
    {
        // Arrange
        RunTestContext context = await CreateRunTestContextAsync();
        context.BuildService.StreamingUsages =
        [
            new UsageDetails { InputTokenCount = 2, OutputTokenCount = 3, TotalTokenCount = 5 },
        ];

        // Act
        await using (IAsyncEnumerator<AgentResponseUpdate> enumerator = context.Service
            .RunStreamingAsync(
                context.OwnerUserId,
                context.AgentId,
                context.ConversationId,
                [new ChatMessage(ChatRole.User, "hello")])
            .GetAsyncEnumerator())
        {
            Assert.IsTrue(await enumerator.MoveNextAsync());
            Assert.IsTrue(await enumerator.MoveNextAsync());
            Assert.IsTrue(enumerator.Current.Contents.OfType<UsageContent>().Any());
        }

        // Assert
        Assert.IsEmpty(context.PersistenceOperations);
        Assert.IsEmpty(context.Messages.AddedMessages);
    }

    /// <summary>验证非流式用量更新失败不会把已完成回复改判失败。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task RunAsync_WhenUsageUpdateFails_ReturnsCompletedResponseAsync()
    {
        // Arrange
        RunTestContext context = await CreateRunTestContextAsync();
        context.BuildService.ResponseUsage = new UsageDetails { InputTokenCount = 2, OutputTokenCount = 3, TotalTokenCount = 5 };
        context.Messages.ThrowOnUsageUpdate = true;

        // Act
        AgentResponse response = await context.Service.RunAsync(
            context.OwnerUserId,
            context.AgentId,
            context.ConversationId,
            [new ChatMessage(ChatRole.User, "hello")]);

        // Assert
        Assert.AreEqual("world", response.Text);
        Assert.HasCount(2, context.Messages.AddedMessages);
        Assert.IsNull(context.Messages.AddedMessages[1].Usage);
    }

    private static async Task<RunTestContext> CreateRunTestContextAsync()
    {
        Guid ownerUserId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        DateTimeOffset now = new(2026, 8, 25, 0, 0, 0, TimeSpan.Zero);
        StubConversationRepository conversations = new(new AgentConversation
        {
            Id = conversationId,
            AgentId = agentId,
            AgentVersionId = versionId,
            OwnerUserId = ownerUserId,
            LastActivityTime = now,
            CreatedTime = now,
            UpdatedTime = now,
        });
        List<string> persistenceOperations = [];
        StubMessageRepository messages = new(persistenceOperations);
        StubSessionStateRepository sessionStates = new(persistenceOperations);
        StubPersistenceProvider persistence = new(new StubAgentRepository(), conversations, messages, sessionStates);
        FixedTimeProvider timeProvider = new(now);
        InkwellChatHistoryProvider historyProvider = new(persistence, timeProvider);
        RecordingBuildService buildService = new(historyProvider);
        AgentConversationService service = new(
            persistence,
            timeProvider,
            buildService,
            new InkwellAgentSessionStateStore(persistence, timeProvider, NullLogger<InkwellAgentSessionStateStore>.Instance),
            NullLogger<AgentConversationService>.Instance);

        return new RunTestContext(ownerUserId, agentId, conversationId, service, buildService, messages, persistenceOperations);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingBuildService(InkwellChatHistoryProvider historyProvider) : IAgentBuildService
    {
        public UsageDetails? ResponseUsage { get; set; }

        public IReadOnlyList<UsageDetails> StreamingUsages { get; set; } = [];

        public List<StubAgent> BuiltAgents { get; } = [];

        public (Guid AgentId, Guid VersionId, Guid UserId)? PublishedVersionRequest { get; private set; }

        public ValueTask<AIAgent> BuildPublishedConversationAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default)
        {
            this.PublishedVersionRequest = (agentId, versionId, requestingUserId);
            StubAgent agent = new(historyProvider, this.ResponseUsage, this.StreamingUsages);
            this.BuiltAgents.Add(agent);
            return ValueTask.FromResult<AIAgent>(agent);
        }

        public ValueTask<AIAgent> BuildDraftAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<AIAgent> BuildDraftTrialAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<AIAgent> BuildPublishedAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<AIAgent> BuildPublishedTrialAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubAgent(
        InkwellChatHistoryProvider historyProvider,
        UsageDetails? responseUsage,
        IReadOnlyList<UsageDetails> streamingUsages) : AIAgent
    {
        private const string SerializedSession = """{"stub":true}""";

        public List<ChatMessage> RequestMessages { get; private set; } = [];

        public List<ChatMessage> InvocationMessages { get; private set; } = [];

        public List<string> DeserializedSessions { get; } = [];

        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AgentSession>(new StubSession());

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(JsonDocument.Parse(SerializedSession).RootElement.Clone());

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            this.DeserializedSessions.Add(serializedState.GetRawText());

            return ValueTask.FromResult<AgentSession>(new StubSession());
        }

        protected override async Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
        {
            this.RequestMessages = [.. messages];
            ChatHistoryProvider.InvokingContext invokingContext = new(this, session, this.RequestMessages);
            this.InvocationMessages = [.. await historyProvider.InvokingAsync(invokingContext, cancellationToken)];
            ChatMessage response = new(ChatRole.Assistant, "world");
            await historyProvider
                .InvokedAsync(new ChatHistoryProvider.InvokedContext(this, session, this.RequestMessages, [response]), cancellationToken)
                .ConfigureAwait(false);
            return new AgentResponse(response) { Usage = responseUsage };
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
            foreach (UsageDetails usage in streamingUsages)
            {
                yield return new AgentResponseUpdate(ChatRole.Assistant, [new UsageContent(usage)]);
            }

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

    private sealed class StubSessionStateRepository(List<string>? persistenceOperations = null) : IAgentSessionStateRepository
    {
        private readonly Dictionary<Guid, AgentSessionState> _states = [];

        public IReadOnlyDictionary<Guid, AgentSessionState> States => this._states;

        public Task<AgentSessionState> AddSessionState(AgentSessionState sessionState, CancellationToken ct = default)
        {
            persistenceOperations?.Add("session-saved");
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

            persistenceOperations?.Add("session-saved");
            this._states[conversationId] = existing with { SessionState = sessionState, UpdatedTime = updatedTime };

            return Task.FromResult(true);
        }

        public Task<bool> DeleteSessionState(Guid conversationId, CancellationToken ct = default) =>
            Task.FromResult(this._states.Remove(conversationId));
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

        public Task<PagedResult<AgentConversationListItem>> ListConversations(Guid agentId, Guid ownerUserId, Pagination pagination, CancellationToken ct = default) => throw new NotSupportedException();

        public Task UpdateConversation(AgentConversation value, CancellationToken ct = default)
        {
            this._conversation = value;
            this.UpdatedConversation = value;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubMessageRepository(List<string>? persistenceOperations = null) : IAgentChatMessageRepository
    {
        public bool ThrowOnUsageUpdate { get; set; }

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

        public Task<bool> UpdateMessageUsage(Guid conversationId, Guid messageId, UsageDetails usage, DateTimeOffset updatedTime, CancellationToken ct = default)
        {
            if (this.ThrowOnUsageUpdate)
            {
                throw new InvalidOperationException("Simulated usage update failure.");
            }

            int index = this.AddedMessages.FindIndex(message => message.ConversationId == conversationId && message.Id == messageId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            persistenceOperations?.Add("usage-updated");
            this.AddedMessages[index] = this.AddedMessages[index] with { Usage = usage, UpdatedTime = updatedTime };
            return Task.FromResult(true);
        }

        public Task<bool> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<int> DeleteMessagesByConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed record class RunTestContext(
        Guid OwnerUserId,
        Guid AgentId,
        Guid ConversationId,
        AgentConversationService Service,
        RecordingBuildService BuildService,
        StubMessageRepository Messages,
        List<string> PersistenceOperations);
}
