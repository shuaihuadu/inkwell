namespace Inkwell;

/// <summary>
/// 持久化端口 facade：仅负责事务边界。具名 Repository（如 <c>IAgentRepository</c>）由业务服务直接构造函数注入——
/// EFCore 层已将 Repository 与 <c>DbContext</c> 注册为同一 Scoped 生命周期，同一 DI Scope 内自动共享同一个
/// <c>DbContext</c> 实例，因此事务边界只需包住 <see cref="ExecuteInTransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>
/// 的回调，回调内直接使用已注入的 Repository 即可参与同一事务，无需额外的 Unit of Work 服务定位器。
/// 唯一实现 <c>EfCorePersistenceProvider</c>（providers/Inkwell.Persistence.EFCore）。
/// </summary>
public interface IPersistenceProvider
{
    /// <summary>在单一事务内执行操作。</summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

    /// <summary>在单一事务内执行操作并返回结果。</summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);
}
