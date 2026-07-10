using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary><see cref="IAgentConversationService"/> 唯一实现；会话 / 消息持久化的完整业务逻辑。</summary>
internal sealed class AgentConversationService(
    IAgentRepository agents,
    IAgentConversationRepository conversations,
    IAgentConversationMessageRepository messages,
    IPersistenceProvider persistence) : IAgentConversationService
{
    public async Task<Guid> StartConversationAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default)
    {
        _ = await agents.GetAgent(agentId, ct).ConfigureAwait(false);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentConversation conversation = new AgentConversation
        {
            Id = Guid.CreateVersion7(),
            AgentId = agentId,
            OwnerUserId = ownerUserId,
            Title = null,
            CreatedTime = now,
            UpdatedTime = now,
        };

        AgentConversation created = await persistence.ExecuteInTransactionAsync(
            innerCt => conversations.AddConversation(conversation, innerCt),
            ct).ConfigureAwait(false);

        return created.Id;
    }

    public async Task<IReadOnlyList<AgentConversationSummary>> ListConversationsAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default)
    {
        List<AgentConversation> all = await PaginationHelper.CollectAllAsync(
            (pagination, innerCt) => conversations.ListConversationsByAgent(
                agentId, ownerUserId, pagination, new SortOrder(nameof(AgentConversation.UpdatedTime), SortDirection.Descending), innerCt),
            ct).ConfigureAwait(false);

        return [.. all.Select(c => new AgentConversationSummary { Id = c.Id, Title = c.Title, LastActivityTime = c.UpdatedTime, CreatedTime = c.CreatedTime })];
    }

    public async Task<IReadOnlyList<AgentChatMessage>> GetHistoryMessagesAsync(Guid conversationId, CancellationToken ct = default)
    {
        _ = await conversations.GetConversation(conversationId, ct).ConfigureAwait(false);

        List<AgentConversationMessage> all = await this.ListAllMessagesAsync(conversationId, ct).ConfigureAwait(false);

        return [.. all.Select(ToAgentChatMessage)];
    }

    public async Task<Guid> AppendMessageAsync(Guid conversationId, AgentChatMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentConversation conversation = await conversations.GetConversation(conversationId, innerCt).ConfigureAwait(false);

            int existingCount = await this.CountMessagesAsync(conversationId, innerCt).ConfigureAwait(false);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            AgentConversationMessage newMessage = new AgentConversationMessage
            {
                Id = Guid.CreateVersion7(),
                ConversationId = conversationId,
                Role = message.Role,
                ContentJson = JsonSerializer.Serialize(message.Content),
                AuthorName = message.AuthorName,
                SequenceNumber = existingCount,
                CreatedTime = now,
                UpdatedTime = now,
            };

            AgentConversationMessage added = await messages.AddMessage(newMessage, innerCt).ConfigureAwait(false);

            string? title = conversation.Title ?? ExtractTitle(message);

            if (title != conversation.Title)
            {
                await conversations.UpdateConversation(conversation with { Title = title, UpdatedTime = now }, innerCt).ConfigureAwait(false);
            }
            else
            {
                await conversations.UpdateConversation(conversation with { UpdatedTime = now }, innerCt).ConfigureAwait(false);
            }

            return added.Id;
        }, ct).ConfigureAwait(false);
    }

    public async Task DeleteMessageAsync(Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken ct = default)
    {
        await persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentConversation conversation = await conversations.GetConversation(conversationId, innerCt).ConfigureAwait(false);

            ValidateOwnership(conversation, actorUserId);

            bool deleted = await messages.DeleteMessage(conversationId, messageId, innerCt).ConfigureAwait(false);

            if (!deleted)
            {
                throw new KeyNotFoundException($"Message '{messageId}' not found in conversation '{conversationId}'.");
            }
        }, ct).ConfigureAwait(false);
    }

    public async Task ClearConversationAsync(Guid conversationId, Guid actorUserId, CancellationToken ct = default)
    {
        await persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentConversation conversation = await conversations.GetConversation(conversationId, innerCt).ConfigureAwait(false);

            ValidateOwnership(conversation, actorUserId);

            await messages.DeleteMessagesByConversation(conversationId, innerCt).ConfigureAwait(false);
            await conversations.UpdateConversation(conversation with { Title = null, UpdatedTime = DateTimeOffset.UtcNow }, innerCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> ListUsedAgentIdsAsync(Guid ownerUserId, CancellationToken ct = default) =>
        await conversations.FindUsedAgentIdsByOwner(ownerUserId, ct).ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<Guid, DateTimeOffset>> GetLastActivityByAgentsAsync(IReadOnlyList<Guid> agentIds, Guid viewerUserId, CancellationToken ct = default) =>
        await conversations.FindLastActivityByAgents(agentIds, viewerUserId, ct).ConfigureAwait(false);

    private Task<List<AgentConversationMessage>> ListAllMessagesAsync(Guid conversationId, CancellationToken ct) =>
        PaginationHelper.CollectAllAsync(
            (pagination, innerCt) => messages.ListMessagesByConversation(
                conversationId, pagination, new SortOrder(nameof(AgentConversationMessage.SequenceNumber), SortDirection.Ascending), innerCt),
            ct);

    private async Task<int> CountMessagesAsync(Guid conversationId, CancellationToken ct)
    {
        List<AgentConversationMessage> all = await this.ListAllMessagesAsync(conversationId, ct).ConfigureAwait(false);

        return all.Count;
    }

    private static AgentChatMessage ToAgentChatMessage(AgentConversationMessage message) => new()
    {
        Role = message.Role,
        Content = JsonSerializer.Deserialize<IReadOnlyList<AIContent>>(message.ContentJson) ?? [],
        AuthorName = message.AuthorName,
    };

    /// <summary>首条 User 角色文本消息前 30 字；无 <see cref="TextContent"/> 时保持 null。</summary>
    private static string? ExtractTitle(AgentChatMessage message)
    {
        if (message.Role != ChatRole.User)
        {
            return null;
        }

        string? text = message.Content.OfType<TextContent>().FirstOrDefault()?.Text;

        return text is null ? null : text[..Math.Min(30, text.Length)];
    }

    private static void ValidateOwnership(AgentConversation conversation, Guid actorUserId)
    {
        if (conversation.OwnerUserId != actorUserId)
        {
            throw new UnauthorizedAccessException($"User '{actorUserId}' is not the owner of conversation '{conversation.Id}'.");
        }
    }
}
