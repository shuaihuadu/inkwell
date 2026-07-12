// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentRepository(InkwellDbContext db) : IAgentRepository
{
    public async Task<AgentDefinition> AddAgent(AgentDefinition agent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        try
        {
            AgentEntity entity = agent.ToEntity();

            db.Set<AgentEntity>().Add(entity);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return entity.ToModel();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException($"Duplicate key: AgentDefinition Id={agent.Id}", ex);
        }
    }

    public async Task UpdateAgent(AgentDefinition agent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        try
        {
            db.Set<AgentEntity>().Update(agent.ToEntity());
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException($"Optimistic concurrency conflict: AgentDefinition Id={agent.Id}", ex);
        }
    }

    public async Task<AgentDefinition> GetAgent(Guid id, CancellationToken ct = default)
    {
        // 读取结果不参与当前上下文跟踪，避免后续 Update(newEntity) 出现同一主键重复跟踪冲突。
        AgentEntity? entity = await db.Set<AgentEntity>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"AgentDefinition not found: id={id}");
    }

    public async Task<bool> DeleteAgent(Guid id, CancellationToken ct = default)
    {
        AgentEntity? entity = await db.Set<AgentEntity>().FindAsync([id], ct).ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        db.Set<AgentEntity>().Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }

    public async Task<PagedResult<AgentDefinition>> ListAgents(Pagination pagination, SortOrder sort, CancellationToken ct = default)
    {
        IOrderedQueryable<AgentEntity> query = db.Set<AgentEntity>().AsNoTracking().ApplySort(sort, FieldSelector);
        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<AgentDefinition> items = await query.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).SelectAsModel().ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<AgentDefinition>(items, total, pagination);
    }

    public async Task<IReadOnlyList<AgentDefinition>> FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default) =>
        await db.Set<AgentEntity>().AsNoTracking().Where(x => x.OwnerUserId == ownerUserId).SelectAsModel().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<AgentDefinition>> FindSharedAgents(Guid excludingOwnerUserId, CancellationToken ct = default) =>
        await db.Set<AgentEntity>().AsNoTracking().Where(x => x.IsShared && x.OwnerUserId != excludingOwnerUserId).SelectAsModel().ToListAsync(ct).ConfigureAwait(false);

    private static System.Linq.Expressions.Expression<Func<AgentEntity, object?>> FieldSelector(string field) => field switch
    {
        nameof(AgentEntity.UpdatedTime) => x => x.UpdatedTime,
        _ => x => x.CreatedTime,
    };
}
