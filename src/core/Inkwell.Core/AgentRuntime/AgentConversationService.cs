// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary><see cref="IAgentConversationService"/> 的默认实现。</summary>
internal sealed class AgentConversationService(
    IPersistenceProvider persistence,
    TimeProvider timeProvider,
    IAgentBuildService buildService) : IAgentConversationService
{
    private readonly IAgentRepository _agents = persistence.GetRepository<IAgentRepository>();
    private readonly IAgentConversationRepository _conversations = persistence.GetRepository<IAgentConversationRepository>();
    private readonly IAgentChatMessageRepository _messages = persistence.GetRepository<IAgentChatMessageRepository>();
    private readonly AgentConversationMessageCommitter _messageCommitter = new(persistence, timeProvider);

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
        return this._messageCommitter.CommitAsync(ownerUserId, agentId, conversationId, executionId, messages, ct);
    }

    /// <inheritdoc />
    public async Task<AgentResponse> RunAsync(
        Guid ownerUserId,
        Guid agentId,
        Guid conversationId,
        IReadOnlyList<ChatMessage> messages,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        RunContext context = await this.PrepareRunAsync(ownerUserId, agentId, conversationId, messages, cancellationToken).ConfigureAwait(false);
        AgentResponse response = await context.Agent
            .RunAsync(context.RunMessages, context.Session, options, cancellationToken)
            .ConfigureAwait(false);
        return response;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
        Guid ownerUserId,
        Guid agentId,
        Guid conversationId,
        IReadOnlyList<ChatMessage> messages,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        RunContext context = await this.PrepareRunAsync(ownerUserId, agentId, conversationId, messages, cancellationToken).ConfigureAwait(false);

        await foreach (AgentResponseUpdate update in context.Agent
            .RunStreamingAsync(context.RunMessages, context.Session, options, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return update;
        }

    }

    private async Task<RunContext> PrepareRunAsync(
        Guid ownerUserId,
        Guid agentId,
        Guid conversationId,
        IReadOnlyList<ChatMessage> requestMessages,
        CancellationToken cancellationToken)
    {
        AgentConversation conversation = await this.GetAuthorizedConversationAsync(ownerUserId, agentId, conversationId, cancellationToken).ConfigureAwait(false);
        AIAgent agent = await buildService
            .BuildPublishedConversationAsync(agentId, conversation.AgentVersionId, ownerUserId, cancellationToken)
            .ConfigureAwait(false);
        string executionId = Guid.CreateVersion7().ToString("D");
        AgentSession session = await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        InkwellChatHistoryProvider.AttachSession(session, conversationId, ownerUserId, agentId, executionId);

        return new RunContext(
            ownerUserId,
            agentId,
            conversationId,
            executionId,
            agent,
            session,
            requestMessages);
    }

    private static string? FindTitle(IReadOnlyList<AgentChatMessage> messages)
    {
        ChatMessage? firstUserMessage = messages.Select(message => message.Message)
            .FirstOrDefault(message => message.Role == ChatRole.User && !string.IsNullOrEmpty(message.Text));
        return firstUserMessage?.Text is { } text ? text[..Math.Min(30, text.Length)] : null;
    }

    private async Task<AgentConversation> GetAuthorizedConversationAsync(Guid ownerUserId, Guid agentId, Guid conversationId, CancellationToken ct)
    {
        AgentConversation conversation = await this._conversations.GetConversation(conversationId, ct).ConfigureAwait(false);

        if (conversation.OwnerUserId != ownerUserId || conversation.AgentId != agentId)
        {
            throw new UnauthorizedAccessException($"User '{ownerUserId}' cannot access conversation '{conversationId}' for agent '{agentId}'.");
        }

        return conversation;
    }

    private sealed record class RunContext(
        Guid OwnerUserId,
        Guid AgentId,
        Guid ConversationId,
        string ExecutionId,
        AIAgent Agent,
        AgentSession Session,
        IReadOnlyList<ChatMessage> RunMessages);

}