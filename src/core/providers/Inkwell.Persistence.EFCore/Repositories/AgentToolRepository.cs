using Inkwell;
using Inkwell.Persistence.EFCore.Entities;
using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentToolRepository(InkwellDbContext db) : IAgentToolRepository
{
    public async Task<AgentToolDefinition> AddTool(AgentToolDefinition tool, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tool);

        try
        {
            AgentToolEntity entity = tool.ToEntity();

            db.Set<AgentToolEntity>().Add(entity);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return entity.ToModel();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException($"Duplicate key: Name={tool.Name}", ex);
        }
    }

    public async Task<AgentToolDefinition> GetTool(Guid id, CancellationToken ct = default)
    {
        AgentToolEntity? entity = await db.Set<AgentToolEntity>().FindAsync([id], ct).ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"AgentToolDefinition not found: id={id}");
    }

    public async Task<AgentToolDefinition> GetToolByName(string name, CancellationToken ct = default)
    {
        AgentToolEntity? entity = await db.Set<AgentToolEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Name == name, ct).ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"AgentToolDefinition not found: name={name}");
    }

    public async Task<PagedResult<AgentToolDefinition>> ListTools(Pagination pagination, SortOrder sort, CancellationToken ct = default)
    {
        IOrderedQueryable<AgentToolEntity> query = db.Set<AgentToolEntity>().AsNoTracking().ApplySort(sort, FieldSelector);
        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<AgentToolDefinition> items = await query.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).SelectAsModel().ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<AgentToolDefinition>(items, total, pagination);
    }

    private static System.Linq.Expressions.Expression<Func<AgentToolEntity, object?>> FieldSelector(string field) => field switch
    {
        nameof(AgentToolEntity.Name) => x => x.Name,
        nameof(AgentToolEntity.UpdatedTime) => x => x.UpdatedTime,
        _ => x => x.CreatedTime,
    };
}
