// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Data;
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary><see cref="IAgentConversationService"/> 的默认实现。</summary>
internal sealed class AgentConversationService(
    IPersistenceProvider persistence,
    TimeProvider timeProvider) : IAgentConversationService
{
    private readonly IAgentRepository _agents = persistence.GetRepository<IAgentRepository>();
    private readonly IAgentConversationRepository _conversations = persistence.GetRepository<IAgentConversationRepository>();
    private readonly IAgentChatMessageRepository _messages = persistence.GetRepository<IAgentChatMessageRepository>();
    private readonly IAgentSessionStateRepository _sessionStates = persistence.GetRepository<IAgentSessionStateRepository>();

    public Task<AgentConversation> CreateConversationAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default) =>
        persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            async innerCt =>
            {
                AgentDefinition agent = await this._agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

                if (agent.OwnerUserId != ownerUserId && !agent.IsShared)
                {
                    throw new UnauthorizedAccessException($"User '{ownerUserId}' cannot access agent '{agentId}'.");
                }

                Guid agentVersionId = agent.CurrentPublishedVersionId
                    ?? throw new InvalidOperationException($"Agent has no published version: agentId={agentId}");
                DateTimeOffset now = timeProvider.GetUtcNow();
                Guid conversationId = Guid.CreateVersion7();
                AgentConversation conversation = new()
                {
                    Id = conversationId,
                    SessionKey = conversationId.ToString("D"),
                    AgentId = agentId,
                    AgentVersionId = agentVersionId,
                    OwnerUserId = ownerUserId,
                    LastActivityTime = now,
                    CreatedTime = now,
                    UpdatedTime = now,
                };
                AgentConversation saved = await this._conversations.AddConversation(conversation, innerCt).ConfigureAwait(false);

                return saved;
            },
            ct);

    public Task<PagedResult<AgentConversationListItem>> ListConversationsAsync(
        Guid agentId,
        Guid ownerUserId,
        Pagination pagination,
        CancellationToken ct = default) =>
        this._conversations.ListConversations(agentId, ownerUserId, pagination, ct);

    public async Task<PagedResult<AgentChatMessage>> GetMessagesAsync(Guid ownerUserId, Guid agentId, Guid conversationId, Pagination pagination, CancellationToken ct = default)
    {
        _ = await this.GetAuthorizedConversationAsync(ownerUserId, agentId, conversationId, ct).ConfigureAwait(false);
        PagedResult<AgentChatMessage> messages = await this._messages.ListMessagesByConversation(conversationId, pagination, ct).ConfigureAwait(false);

        return messages;
    }

    public async Task DeleteMessageAsync(Guid ownerUserId, Guid agentId, Guid conversationId, Guid messageId, CancellationToken ct = default)
    {
        await persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            async innerCt =>
            {
                DateTimeOffset now = timeProvider.GetUtcNow();
                AgentConversation conversation = await this.GetAuthorizedConversationAsync(ownerUserId, agentId, conversationId, innerCt).ConfigureAwait(false);
                bool deleted = await this._messages.DeleteMessage(conversationId, messageId, innerCt).ConfigureAwait(false);
                if (!deleted)
                {
                    throw new KeyNotFoundException($"Agent chat message not found: id={messageId}");
                }

                _ = await this._sessionStates.DeleteSessionStateByConversation(conversationId, innerCt).ConfigureAwait(false);
                IReadOnlyList<AgentChatMessage> remainingMessages = await this._messages.ListAllMessagesByConversation(conversationId, innerCt).ConfigureAwait(false);
                AgentConversation updated = conversation with
                {
                    Title = FindTitle(remainingMessages),
                    LastCommittedRunId = null,
                    LastActivityTime = remainingMessages.Count == 0 ? now : remainingMessages.Max(message => message.UpdatedTime),
                    UpdatedTime = now,
                };
                await this._conversations.UpdateConversation(updated, innerCt).ConfigureAwait(false);
            },
            ct).ConfigureAwait(false);
    }

    public async Task ClearConversationAsync(Guid ownerUserId, Guid agentId, Guid conversationId, CancellationToken ct = default)
    {
        await persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            async innerCt =>
            {
                DateTimeOffset now = timeProvider.GetUtcNow();
                AgentConversation conversation = await this.GetAuthorizedConversationAsync(ownerUserId, agentId, conversationId, innerCt).ConfigureAwait(false);
                _ = await this._messages.DeleteMessagesByConversation(conversationId, innerCt).ConfigureAwait(false);
                _ = await this._sessionStates.DeleteSessionStateByConversation(conversationId, innerCt).ConfigureAwait(false);
                AgentConversation cleared = conversation with
                {
                    Title = null,
                    LastCommittedRunId = null,
                    LastActivityTime = now,
                    UpdatedTime = now,
                };
                await this._conversations.UpdateConversation(cleared, innerCt).ConfigureAwait(false);
            },
            ct).ConfigureAwait(false);
    }

    public async Task DeleteConversationAsync(Guid ownerUserId, Guid agentId, Guid conversationId, CancellationToken ct = default)
    {
        _ = await this.GetAuthorizedConversationAsync(ownerUserId, agentId, conversationId, ct).ConfigureAwait(false);
        bool deleted = await persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            innerCt => this._conversations.DeleteConversation(conversationId, innerCt),
            ct).ConfigureAwait(false);

        if (!deleted)
        {
            throw new KeyNotFoundException($"Agent conversation not found: id={conversationId}");
        }
    }

    public Task<AgentChatMessageCommitResult> CommitRunMessagesAsync(
        Guid ownerUserId,
        Guid agentId,
        Guid conversationId,
        string executionId,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionId);
        ArgumentNullException.ThrowIfNull(messages);
        return persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            async innerCt =>
            {
                DateTimeOffset now = timeProvider.GetUtcNow();
                AgentConversation conversation = await this.GetAuthorizedConversationAsync(ownerUserId, agentId, conversationId, innerCt).ConfigureAwait(false);
                IReadOnlyList<AgentChatMessage> existing = await this._messages.ListMessagesByRun(conversationId, executionId, innerCt).ConfigureAwait(false);
                if (existing.Count > 0)
                {
                    return MessageBatchesEqual(existing, messages)
                        ? AgentChatMessageCommitResult.AlreadyCommitted
                        : AgentChatMessageCommitResult.Conflict;
                }

                List<AgentChatMessage> batch = new(messages.Count);
                for (int index = 0; index < messages.Count; index++)
                {
                    batch.Add(new AgentChatMessage
                    {
                        Id = Guid.CreateVersion7(),
                        ConversationId = conversationId,
                        RunId = executionId,
                        RunMessageIndex = index,
                        Message = messages[index],
                        SequenceNumber = 0,
                        CreatedTime = now,
                        UpdatedTime = now,
                    });
                }

                _ = await this._messages.AddMessages(batch, innerCt).ConfigureAwait(false);
                IReadOnlyList<AgentChatMessage> allMessages = await this._messages.ListAllMessagesByConversation(conversationId, innerCt).ConfigureAwait(false);
                AgentConversation updated = conversation with
                {
                    Title = FindTitle(allMessages),
                    LastCommittedRunId = executionId,
                    LastActivityTime = now,
                    UpdatedTime = now,
                };
                await this._conversations.UpdateConversation(updated, innerCt).ConfigureAwait(false);
                return AgentChatMessageCommitResult.Committed;
            },
            ct);
    }

    public Task<AgentSessionStateSaveResult> SaveSessionStateAsync(
        Guid ownerUserId,
        Guid agentId,
        AgentSessionState state,
        string executionId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionId);
        return persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            async innerCt =>
            {
                _ = await this.GetAuthorizedConversationAsync(ownerUserId, agentId, state.ConversationId, innerCt).ConfigureAwait(false);

                AgentSessionState? existing = await this._sessionStates.GetSessionStateOrDefault(state.ConversationId, innerCt).ConfigureAwait(false);
                if (state.Revision != (existing?.Revision ?? 0) + 1)
                {
                    return AgentSessionStateSaveResult.ConcurrencyConflict;
                }

                if (existing is null)
                {
                    await this._sessionStates.AddSessionState(state, innerCt).ConfigureAwait(false);
                }
                else
                {
                    await this._sessionStates.UpdateSessionState(state, innerCt).ConfigureAwait(false);
                }

                return AgentSessionStateSaveResult.Saved;
            },
            ct);
    }

    private static string? FindTitle(IReadOnlyList<AgentChatMessage> messages)
    {
        ChatMessage? firstUserMessage = messages.Select(message => message.Message)
            .FirstOrDefault(message => message.Role == ChatRole.User && !string.IsNullOrEmpty(message.Text));
        return firstUserMessage?.Text is { } text ? text[..Math.Min(30, text.Length)] : null;
    }

    private static bool MessageBatchesEqual(IReadOnlyList<AgentChatMessage> existing, IReadOnlyList<ChatMessage> expected) =>
        existing.Count == expected.Count
        && existing.Select((message, index) => JsonElement.DeepEquals(
            JsonSerializer.SerializeToElement(message.Message),
            JsonSerializer.SerializeToElement(expected[index]))).All(equal => equal);

    private async Task<AgentConversation> GetAuthorizedConversationAsync(Guid ownerUserId, Guid agentId, Guid conversationId, CancellationToken ct)
    {
        AgentConversation conversation = await this._conversations.GetConversation(conversationId, ct).ConfigureAwait(false);

        if (conversation.OwnerUserId != ownerUserId || conversation.AgentId != agentId)
        {
            throw new UnauthorizedAccessException($"User '{ownerUserId}' cannot access conversation '{conversationId}' for agent '{agentId}'.");
        }

        return conversation;
    }

}