namespace Inkwell;

/// <summary>
/// 缓存端口 facade。Provider：InMemory（dev/test 默认）/ Redis（prod，ADR-016）。
/// </summary>
public interface ICacheProvider
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    Task<long> IncrementAsync(string key, long delta, CancellationToken ct = default);

    Task<bool> TryAcquireLockAsync(string key, TimeSpan ttl, CancellationToken ct = default);

    Task ReleaseLockAsync(string key, CancellationToken ct = default);
}
