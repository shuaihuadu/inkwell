namespace Inkwell;

/// <summary>
/// 持久化提供程序接口
/// 定义所有实体的 CRUD 操作契约
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IPersistenceProvider<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体实例，不存在时返回 null</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体集合</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加实体
    /// </summary>
    /// <param name="entity">要添加的实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">要更新的实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功删除</returns>
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据条件查询实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的实体集合</returns>
    Task<IReadOnlyList<TEntity>> QueryAsync(Func<TEntity, bool> predicate, CancellationToken cancellationToken = default);
}
