// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentSessionRepository(InkwellDbContext db) : IAgentSessionRepository
{
    public async Task<AgentSessionDefinition> AddSession(AgentSessionDefinition sessionDefinition, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sessionDefinition);

        AgentSessionEntity entity = sessionDefinition.ToEntity();

        db.Set<AgentSessionEntity>().Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.ToModel();
    }

    public async Task<AgentSessionDefinition> GetSession(Guid id, CancellationToken ct = default)
    {
        // AsNoTracking：同 AgentRepository.GetAgent 的说明，避免与 UpdateSession 产生重复追踪冲突。
        AgentSessionEntity? entity = await db.Set<AgentSessionEntity>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"Agent session not found: id={id}");
    }

    public async Task<AgentSessionDefinition> UpdateSession(AgentSessionDefinition sessionDefinition, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sessionDefinition);

        try
        {
            AgentSessionEntity entity = sessionDefinition.ToEntity();

            db.Set<AgentSessionEntity>().Update(entity);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return entity.ToModel();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException($"Optimistic concurrency conflict: AgentSessionDefinition Id={sessionDefinition.Id}", ex);
        }
    }

    public async Task<PagedResult<AgentSessionDefinition>> ListSessionsByAgent(Guid agentId, Guid ownerUserId, Pagination pagination, SortOrder sort, CancellationToken ct = default)
    {
        IOrderedQueryable<AgentSessionEntity> query = db.Set<AgentSessionEntity>().AsNoTracking()
            .Where(x => x.AgentId == agentId && x.OwnerUserId == ownerUserId)
            .ApplySort(sort, FieldSelector);

        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<AgentSessionEntity> entities = await query.Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        List<AgentSessionDefinition> items = [.. entities.Select(entity => entity.ToModel())];

        return new PagedResult<AgentSessionDefinition>(items, total, pagination);
    }

    public async Task<IReadOnlyList<Guid>> FindUsedAgentIdsByOwner(Guid ownerUserId, CancellationToken ct = default) =>
        await db.Set<AgentSessionEntity>().AsNoTracking()
            .Where(c => c.OwnerUserId == ownerUserId && db.Set<AgentChatMessageEntity>().Any(m => m.SessionId == c.Id))
            .Select(c => c.AgentId)
            .Distinct()
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<Guid, DateTimeOffset>> FindLastActivityByAgents(IReadOnlyList<Guid> agentIds, Guid viewerUserId, CancellationToken ct = default)
    {
        var grouped = await db.Set<AgentSessionEntity>().AsNoTracking()
            .Where(x => agentIds.Contains(x.AgentId) && x.OwnerUserId == viewerUserId)
            .GroupBy(x => x.AgentId)
            .Select(g => new { AgentId = g.Key, LastActivity = g.Max(x => x.UpdatedTime) })
            .ToListAsync(ct).ConfigureAwait(false);

        return grouped.ToDictionary(x => x.AgentId, x => x.LastActivity);
    }

    private static System.Linq.Expressions.Expression<Func<AgentSessionEntity, object?>> FieldSelector(string field) => field switch
    {
        nameof(AgentSessionEntity.CreatedTime) => x => x.CreatedTime,
        _ => x => x.UpdatedTime,
    };
}
