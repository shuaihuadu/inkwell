// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentConversationMessageRepository(InkwellDbContext db) : IAgentConversationMessageRepository
{
    public async Task<AgentChatMessage> AddMessage(AgentChatMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        AgentConversationMessageEntity entity = message.ToEntity();

        db.Set<AgentConversationMessageEntity>().Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.ToModel();
    }

    public async Task<PagedResult<AgentChatMessage>> ListMessagesByConversation(Guid conversationId, Pagination pagination, SortOrder sort, CancellationToken ct = default)
    {
        IOrderedQueryable<AgentConversationMessageEntity> query = db.Set<AgentConversationMessageEntity>().AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .ApplySort(sort, FieldSelector);

        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<AgentConversationMessageEntity> entities = await query.Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        List<AgentChatMessage> items = [.. entities.Select(entity => entity.ToModel())];

        return new PagedResult<AgentChatMessage>(items, total, pagination);
    }

    public async Task<IReadOnlyList<ChatMessage>> ListHistoryMessagesAsync(Guid conversationId, int? maxMessages = null, CancellationToken ct = default)
    {
        IQueryable<AgentConversationMessageEntity> query = db.Set<AgentConversationMessageEntity>()
            .AsNoTracking()
            .Where(entity => entity.ConversationId == conversationId)
            .OrderByDescending(entity => entity.SequenceNumber);

        if (maxMessages is > 0)
        {
            query = query.Take(maxMessages.Value);
        }

        List<AgentConversationMessageEntity> entities = await query.ToListAsync(ct).ConfigureAwait(false);
        entities.Reverse();

        return [.. entities.Select(entity => entity.ToModel().Message)];
    }

    public async Task<IReadOnlyList<AgentChatMessage>> AppendMessagesAsync(Guid conversationId, IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (messages.Count == 0)
        {
            return [];
        }

        bool conversationExists = await db.Set<AgentConversationEntity>()
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == conversationId, ct)
            .ConfigureAwait(false);

        if (!conversationExists)
        {
            throw new KeyNotFoundException($"Conversation not found: id={conversationId}");
        }

        int lastSequenceNumber = await db.Set<AgentConversationMessageEntity>()
            .Where(entity => entity.ConversationId == conversationId)
            .MaxAsync(entity => (int?)entity.SequenceNumber, ct)
            .ConfigureAwait(false) ?? 0;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<AgentConversationMessageEntity> entities = new(messages.Count);

        for (int index = 0; index < messages.Count; index++)
        {
            AgentChatMessage message = new()
            {
                Id = Guid.CreateVersion7(),
                SessionId = conversationId,
                Message = messages[index],
                SequenceNumber = checked(lastSequenceNumber + index + 1),
                CreatedTime = now,
                UpdatedTime = now,
            };
            entities.Add(message.ToEntity());
        }

        db.Set<AgentConversationMessageEntity>().AddRange(entities);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return [.. entities.Select(entity => entity.ToModel())];
    }

    public async Task<bool> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken ct = default)
    {
        AgentConversationMessageEntity? entity = await db.Set<AgentConversationMessageEntity>()
            .FirstOrDefaultAsync(x => x.Id == messageId && x.ConversationId == conversationId, ct).ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        db.Set<AgentConversationMessageEntity>().Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }

    public async Task<int> DeleteMessagesByConversation(Guid conversationId, CancellationToken ct = default) =>
        await db.Set<AgentConversationMessageEntity>().Where(x => x.ConversationId == conversationId).ExecuteDeleteAsync(ct).ConfigureAwait(false);

    private static System.Linq.Expressions.Expression<Func<AgentConversationMessageEntity, object?>> FieldSelector(string field) => field switch
    {
        nameof(AgentConversationMessageEntity.CreatedTime) => x => x.CreatedTime,
        _ => x => x.SequenceNumber,
    };
}
