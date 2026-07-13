// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentSessionMessageRepository(InkwellDbContext db) : IAgentSessionMessageRepository
{
    public async Task<AgentChatMessage> AddMessage(AgentChatMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        AgentChatMessageEntity entity = message.ToEntity();

        db.Set<AgentChatMessageEntity>().Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.ToModel();
    }

    public async Task<PagedResult<AgentChatMessage>> ListMessagesBySession(Guid sessionId, Pagination pagination, SortOrder sort, CancellationToken ct = default)
    {
        IOrderedQueryable<AgentChatMessageEntity> query = db.Set<AgentChatMessageEntity>().AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .ApplySort(sort, FieldSelector);

        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<AgentChatMessageEntity> entities = await query.Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        List<AgentChatMessage> items = [.. entities.Select(entity => entity.ToModel())];

        return new PagedResult<AgentChatMessage>(items, total, pagination);
    }

    public async Task<IReadOnlyList<ChatMessage>> ListHistoryMessagesAsync(Guid sessionId, int? maxMessages = null, CancellationToken ct = default)
    {
        IQueryable<AgentChatMessageEntity> query = db.Set<AgentChatMessageEntity>()
            .AsNoTracking()
            .Where(entity => entity.SessionId == sessionId)
            .OrderByDescending(entity => entity.SequenceNumber);

        if (maxMessages is > 0)
        {
            query = query.Take(maxMessages.Value);
        }

        List<AgentChatMessageEntity> entities = await query.ToListAsync(ct).ConfigureAwait(false);
        entities.Reverse();

        return [.. entities.Select(entity => entity.ToModel().Message)];
    }

    public async Task<IReadOnlyList<AgentChatMessage>> AppendMessagesAsync(Guid sessionId, IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (messages.Count == 0)
        {
            return [];
        }

        bool sessionExists = await db.Set<AgentSessionEntity>()
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == sessionId, ct)
            .ConfigureAwait(false);

        if (!sessionExists)
        {
            throw new KeyNotFoundException($"Agent session not found: id={sessionId}");
        }

        int lastSequenceNumber = await db.Set<AgentChatMessageEntity>()
            .Where(entity => entity.SessionId == sessionId)
            .MaxAsync(entity => (int?)entity.SequenceNumber, ct)
            .ConfigureAwait(false) ?? 0;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<AgentChatMessageEntity> entities = new(messages.Count);

        for (int index = 0; index < messages.Count; index++)
        {
            AgentChatMessage message = new()
            {
                Id = Guid.CreateVersion7(),
                SessionId = sessionId,
                Message = messages[index],
                SequenceNumber = checked(lastSequenceNumber + index + 1),
                CreatedTime = now,
                UpdatedTime = now,
            };
            entities.Add(message.ToEntity());
        }

        db.Set<AgentChatMessageEntity>().AddRange(entities);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return [.. entities.Select(entity => entity.ToModel())];
    }

    public async Task<bool> DeleteMessage(Guid sessionId, Guid messageId, CancellationToken ct = default)
    {
        AgentChatMessageEntity? entity = await db.Set<AgentChatMessageEntity>()
            .FirstOrDefaultAsync(x => x.Id == messageId && x.SessionId == sessionId, ct).ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        db.Set<AgentChatMessageEntity>().Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }

    public async Task<int> DeleteMessagesBySession(Guid sessionId, CancellationToken ct = default) =>
        await db.Set<AgentChatMessageEntity>().Where(x => x.SessionId == sessionId).ExecuteDeleteAsync(ct).ConfigureAwait(false);

    private static System.Linq.Expressions.Expression<Func<AgentChatMessageEntity, object?>> FieldSelector(string field) => field switch
    {
        nameof(AgentChatMessageEntity.CreatedTime) => x => x.CreatedTime,
        _ => x => x.SequenceNumber,
    };
}
