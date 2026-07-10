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
        List<AgentChatMessage> items = await query.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).SelectAsModel().ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<AgentChatMessage>(items, total, pagination);
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
