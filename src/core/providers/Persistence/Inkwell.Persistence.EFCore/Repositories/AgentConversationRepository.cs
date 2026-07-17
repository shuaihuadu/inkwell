// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentConversationRepository(InkwellDbContext db) : IAgentConversationRepository
{
    public async Task<AgentConversation> AddConversation(AgentConversation conversation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        AgentConversationEntity entity = conversation.ToEntity();
        db.Set<AgentConversationEntity>().Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity.ToModel();
    }

    public async Task<AgentConversation> GetConversation(Guid conversationId, CancellationToken ct = default)
    {
        AgentConversationEntity? entity = await db.Set<AgentConversationEntity>().AsNoTracking()
            .FirstOrDefaultAsync(conversation => conversation.Id == conversationId, ct).ConfigureAwait(false);
        return entity?.ToModel() ?? throw new KeyNotFoundException($"Agent conversation not found: id={conversationId}");
    }

    public async Task<AgentConversation> GetConversationBySessionKey(string sessionKey, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionKey);
        AgentConversationEntity? entity = await db.Set<AgentConversationEntity>().AsNoTracking()
            .FirstOrDefaultAsync(conversation => conversation.SessionKey == sessionKey, ct).ConfigureAwait(false);
        return entity?.ToModel() ?? throw new KeyNotFoundException($"Agent conversation not found: sessionKey={sessionKey}");
    }

    public async Task<PagedResult<AgentConversationListItem>> ListConversations(Guid agentId, Guid ownerUserId, Pagination pagination, CancellationToken ct = default)
    {
        IQueryable<AgentConversationEntity> query = db.Set<AgentConversationEntity>().AsNoTracking()
            .Where(conversation => conversation.AgentId == agentId && conversation.OwnerUserId == ownerUserId);
        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<AgentConversationListItem> items = await query.OrderByDescending(conversation => conversation.LastActivityTime)
            .ThenByDescending(conversation => conversation.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(conversation => new AgentConversationListItem
            {
                Id = conversation.Id,
                AgentVersionId = conversation.AgentVersionId,
                Title = conversation.Title,
                LastActivityTime = conversation.LastActivityTime,
                CreatedTime = conversation.CreatedTime,
            })
            .ToListAsync(ct).ConfigureAwait(false);
        return new PagedResult<AgentConversationListItem>(items, total, pagination);
    }

    public async Task UpdateConversation(AgentConversation conversation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        db.Set<AgentConversationEntity>().Update(conversation.ToEntity());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteConversation(Guid conversationId, CancellationToken ct = default)
    {
        int changed = await db.Set<AgentConversationEntity>()
            .Where(conversation => conversation.Id == conversationId)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);
        return changed == 1;
    }
}
