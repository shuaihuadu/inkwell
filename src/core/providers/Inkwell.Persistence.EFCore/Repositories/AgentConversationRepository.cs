// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentConversationRepository(InkwellDbContext db) : IAgentConversationRepository
{
    public async Task<AgentSessionDefinition> AddConversation(AgentSessionDefinition conversation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        AgentConversationEntity entity = conversation.ToEntity();

        db.Set<AgentConversationEntity>().Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.ToModel();
    }

    public async Task<AgentSessionDefinition> GetConversation(Guid id, CancellationToken ct = default)
    {
        // AsNoTracking：同 AgentRepository.GetAgent 的说明，避免与 UpdateConversation 产生重复追踪冲突。
        AgentConversationEntity? entity = await db.Set<AgentConversationEntity>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"Conversation not found: id={id}");
    }

    public async Task<AgentSessionDefinition> UpdateConversation(AgentSessionDefinition conversation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        try
        {
            AgentConversationEntity entity = conversation.ToEntity();

            db.Set<AgentConversationEntity>().Update(entity);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return entity.ToModel();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException($"Optimistic concurrency conflict: Conversation Id={conversation.Id}", ex);
        }
    }

    public async Task<PagedResult<AgentSessionDefinition>> ListConversationsByAgent(Guid agentId, Guid ownerUserId, Pagination pagination, SortOrder sort, CancellationToken ct = default)
    {
        IOrderedQueryable<AgentConversationEntity> query = db.Set<AgentConversationEntity>().AsNoTracking()
            .Where(x => x.AgentId == agentId && x.OwnerUserId == ownerUserId)
            .ApplySort(sort, FieldSelector);

        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<AgentSessionDefinition> items = await query.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).SelectAsModel().ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<AgentSessionDefinition>(items, total, pagination);
    }

    public async Task<IReadOnlyList<Guid>> FindUsedAgentIdsByOwner(Guid ownerUserId, CancellationToken ct = default) =>
        await db.Set<AgentConversationEntity>().AsNoTracking()
            .Where(c => c.OwnerUserId == ownerUserId && db.Set<AgentConversationMessageEntity>().Any(m => m.ConversationId == c.Id))
            .Select(c => c.AgentId)
            .Distinct()
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<Guid, DateTimeOffset>> FindLastActivityByAgents(IReadOnlyList<Guid> agentIds, Guid viewerUserId, CancellationToken ct = default)
    {
        var grouped = await db.Set<AgentConversationEntity>().AsNoTracking()
            .Where(x => agentIds.Contains(x.AgentId) && x.OwnerUserId == viewerUserId)
            .GroupBy(x => x.AgentId)
            .Select(g => new { AgentId = g.Key, LastActivity = g.Max(x => x.UpdatedTime) })
            .ToListAsync(ct).ConfigureAwait(false);

        return grouped.ToDictionary(x => x.AgentId, x => x.LastActivity);
    }

    private static System.Linq.Expressions.Expression<Func<AgentConversationEntity, object?>> FieldSelector(string field) => field switch
    {
        nameof(AgentConversationEntity.CreatedTime) => x => x.CreatedTime,
        _ => x => x.UpdatedTime,
    };
}
