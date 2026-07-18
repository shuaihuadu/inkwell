// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentSkillRepository(InkwellDbContext db) : IAgentSkillRepository
{
    public async Task<AgentSkillDefinition> AddSkill(AgentSkillDefinition skill, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(skill);

        AgentSkillEntity entity = skill.ToEntity();

        db.Set<AgentSkillEntity>().Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.ToModel();
    }

    public async Task<AgentSkillDefinition> UpdateSkill(
        AgentSkillDefinition skill,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(skill);

        AgentSkillEntity entity = skill.ToEntity();
        db.Set<AgentSkillEntity>().Attach(entity);
        db.Entry(entity).State = EntityState.Modified;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.ToModel();
    }

    public async Task<AgentSkillDefinition> GetSkill(Guid id, CancellationToken ct = default)
    {
        AgentSkillEntity? entity = await db.Set<AgentSkillEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, ct)
            .ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"AgentSkillDefinition not found: id={id}");
    }

    public async Task<bool> DeleteSkill(Guid id, CancellationToken ct = default)
    {
        AgentSkillEntity? entity = await db.Set<AgentSkillEntity>().FindAsync([id], ct).ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        db.Set<AgentSkillEntity>().Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }

    public async Task<PagedResult<AgentSkillDefinition>> ListSkills(Pagination pagination, SortOrder sort, CancellationToken ct = default)
    {
        IOrderedQueryable<AgentSkillEntity> query = db.Set<AgentSkillEntity>().AsNoTracking().ApplySort(sort, FieldSelector);
        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<AgentSkillEntity> entities = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        List<AgentSkillDefinition> items = [.. entities.Select(AgentSkillMappingExtensions.ToModel)];

        return new PagedResult<AgentSkillDefinition>(items, total, pagination);
    }

    private static System.Linq.Expressions.Expression<Func<AgentSkillEntity, object?>> FieldSelector(string field) => field switch
    {
        nameof(AgentSkillEntity.Name) => x => x.Name,
        nameof(AgentSkillEntity.UpdatedTime) => x => x.UpdatedTime,
        _ => x => x.CreatedTime,
    };
}
