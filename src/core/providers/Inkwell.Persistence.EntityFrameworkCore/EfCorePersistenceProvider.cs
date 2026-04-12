using Inkwell;
using Microsoft.EntityFrameworkCore;

namespace Inkwell.Persistence.EntityFrameworkCore;

/// <summary>
/// 基于 EF Core 的泛型持久化提供程序基类
/// TEntity 是 Entity（数据库实体），TModel 是 Record（数据载体）
/// </summary>
/// <typeparam name="TEntity">数据库实体类型</typeparam>
/// <typeparam name="TModel">数据载体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class EfCorePersistenceProvider<TEntity, TModel, TKey>(InkwellDbContext dbContext)
    : IPersistenceProvider<TModel, TKey> where TEntity : class where TModel : class where TKey : notnull
{
    /// <summary>
    /// 获取数据库上下文
    /// </summary>
    protected InkwellDbContext DbContext { get; } = dbContext;

    /// <summary>
    /// 将 Model 转换为 Entity
    /// </summary>
    protected abstract TEntity ToEntity(TModel model);

    /// <summary>
    /// 将 Entity 转换为 Model
    /// </summary>
    protected abstract TModel ToModel(TEntity entity);

    /// <summary>
    /// 从 Model 获取主键
    /// </summary>
    protected abstract TKey GetKey(TModel model);

    /// <inheritdoc />
    public async Task<TModel?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        TEntity? entity = await this.DbContext.Set<TEntity>().FindAsync([id], cancellationToken).ConfigureAwait(false);
        return entity is not null ? this.ToModel(entity) : default;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<TEntity> entities = await this.DbContext.Set<TEntity>().ToListAsync(cancellationToken).ConfigureAwait(false);
        return entities.Select(this.ToModel).ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public async Task AddAsync(TModel model, CancellationToken cancellationToken = default)
    {
        TEntity entity = this.ToEntity(model);
        this.DbContext.Set<TEntity>().Add(entity);
        await this.DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        TKey key = this.GetKey(model);
        TEntity? existing = await this.DbContext.Set<TEntity>().FindAsync([key], cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            TEntity updated = this.ToEntity(model);
            this.DbContext.Entry(existing).CurrentValues.SetValues(updated);
            await this.DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        TEntity? entity = await this.DbContext.Set<TEntity>().FindAsync([id], cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return false;
        }

        this.DbContext.Set<TEntity>().Remove(entity);
        await this.DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TModel>> QueryAsync(Func<TModel, bool> predicate, CancellationToken cancellationToken = default)
    {
        List<TEntity> entities = await this.DbContext.Set<TEntity>().ToListAsync(cancellationToken).ConfigureAwait(false);
        return entities.Select(this.ToModel).Where(predicate).ToList().AsReadOnly();
    }
}
