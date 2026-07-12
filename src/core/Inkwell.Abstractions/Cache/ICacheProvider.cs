// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 缓存端口 facade。Provider：InMemory（dev/test 默认）/ Redis（prod，ADR-016）。
/// </summary>
public interface ICacheProvider
{
    /// <summary>
    /// 获取指定键的缓存值。
    /// </summary>
    /// <typeparam name="T">缓存值类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>缓存值；键不存在时为默认值。</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// 设置指定键的缓存值。
    /// </summary>
    /// <typeparam name="T">缓存值类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="value">缓存值。</param>
    /// <param name="options">缓存项配置。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default);

    /// <summary>
    /// 删除指定键的缓存项。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// 检查指定缓存键是否存在。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>键存在时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// 将指定键的整数值增加给定增量。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="delta">增量。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>递增后的值。</returns>
    Task<long> IncrementAsync(string key, long delta, CancellationToken ct = default);

    /// <summary>
    /// 尝试获取指定键的锁。
    /// </summary>
    /// <param name="key">锁键。</param>
    /// <param name="ttl">锁的生存时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>获取成功时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    Task<bool> TryAcquireLockAsync(string key, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// 释放指定键的锁。
    /// </summary>
    /// <param name="key">锁键。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ReleaseLockAsync(string key, CancellationToken ct = default);
}
