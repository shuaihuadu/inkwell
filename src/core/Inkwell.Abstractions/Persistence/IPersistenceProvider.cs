// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Data;

namespace Inkwell;

/// <summary>
/// 持久化端口 facade：提供具名 Repository 获取和事务边界。
/// 唯一实现 <c>EfCorePersistenceProvider</c>（providers/Inkwell.Persistence.EFCore）。
/// </summary>
public interface IPersistenceProvider
{
    /// <summary>获取与当前持久化作用域关联的具名 Repository。</summary>
    /// <typeparam name="TRepository">Repository 接口类型。</typeparam>
    /// <returns>当前作用域中的 Repository 实例。</returns>
    TRepository GetRepository<TRepository>() where TRepository : notnull;

    /// <summary>在单一事务内执行操作。</summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

    /// <summary>在单一事务内执行操作并返回结果。</summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);

    /// <summary>使用指定隔离级别在单一事务内执行操作。</summary>
    /// <param name="isolationLevel">事务隔离级别。</param>
    /// <param name="action">事务内执行的操作。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct = default);

    /// <summary>使用指定隔离级别在单一事务内执行操作并返回结果。</summary>
    /// <typeparam name="TResult">操作结果类型。</typeparam>
    /// <param name="isolationLevel">事务隔离级别。</param>
    /// <param name="action">事务内执行的操作。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>事务操作结果。</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);
}
