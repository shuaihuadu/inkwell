// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentChatMessageRepository(InkwellDbContext db) : IAgentChatMessageRepository
{
    public async Task<PagedResult<AgentChatMessage>> ListMessagesByConversation(Guid conversationId, Pagination pagination, CancellationToken ct = default)
    {
        IQueryable<AgentChatMessageEntity> query = db.Set<AgentChatMessageEntity>().AsNoTracking().Where(message => message.ConversationId == conversationId);
        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<AgentChatMessageEntity> entities = await query.OrderBy(message => message.SequenceNumber)
            .Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToListAsync(ct).ConfigureAwait(false);
        return new PagedResult<AgentChatMessage>([.. entities.Select(AgentChatMessageMappingExtensions.ToModel)], total, pagination);
    }

    public async Task<IReadOnlyList<ChatMessage>> ListHistoryMessagesAsync(Guid conversationId, int? maxMessages = null, CancellationToken ct = default)
    {
        IQueryable<AgentChatMessageEntity> query = db.Set<AgentChatMessageEntity>().AsNoTracking()
            .Where(message => message.ConversationId == conversationId).OrderByDescending(message => message.SequenceNumber);
        if (maxMessages is > 0)
        {
            query = query.Take(maxMessages.Value);
        }

        List<AgentChatMessageEntity> entities = await query.ToListAsync(ct).ConfigureAwait(false);
        entities.Reverse();
        return [.. entities.Select(entity => entity.ToModel().Message)];
    }

    public async Task<IReadOnlyList<AgentChatMessage>> ListAllMessagesByConversation(Guid conversationId, CancellationToken ct = default)
    {
        List<AgentChatMessageEntity> entities = await db.Set<AgentChatMessageEntity>().AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.SequenceNumber)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return [.. entities.Select(AgentChatMessageMappingExtensions.ToModel)];
    }

    public async Task<IReadOnlyList<AgentChatMessage>> ListMessagesByRun(Guid conversationId, string runId, CancellationToken ct = default)
    {
        List<AgentChatMessageEntity> entities = await db.Set<AgentChatMessageEntity>().AsNoTracking()
            .Where(message => message.ConversationId == conversationId && message.RunId == runId)
            .OrderBy(message => message.RunMessageIndex)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return [.. entities.Select(AgentChatMessageMappingExtensions.ToModel)];
    }

    public async Task<IReadOnlyList<AgentChatMessage>> AddMessages(IReadOnlyList<AgentChatMessage> messages, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count == 0)
        {
            return [];
        }

        List<AgentChatMessageEntity> entities = [];
        foreach (IGrouping<Guid, AgentChatMessage> group in messages.GroupBy(message => message.ConversationId))
        {
            int lastSequence = await db.Set<AgentChatMessageEntity>()
                .Where(message => message.ConversationId == group.Key)
                .MaxAsync(message => (int?)message.SequenceNumber, ct)
                .ConfigureAwait(false) ?? 0;
            int index = 0;
            foreach (AgentChatMessage message in group)
            {
                index++;
                entities.Add((message with { SequenceNumber = checked(lastSequence + index) }).ToEntity());
            }
        }

        db.Set<AgentChatMessageEntity>().AddRange(entities);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return [.. entities.Select(AgentChatMessageMappingExtensions.ToModel)];
    }

    public async Task<bool> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken ct = default) =>
        await db.Set<AgentChatMessageEntity>()
            .Where(message => message.ConversationId == conversationId && message.Id == messageId)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false) == 1;

    public async Task<int> DeleteMessagesByConversation(Guid conversationId, CancellationToken ct = default) =>
        await db.Set<AgentChatMessageEntity>().Where(message => message.ConversationId == conversationId).ExecuteDeleteAsync(ct).ConfigureAwait(false);

}