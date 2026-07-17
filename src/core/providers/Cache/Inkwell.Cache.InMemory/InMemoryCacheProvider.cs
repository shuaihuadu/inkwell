// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Collections.Concurrent;

namespace Inkwell.Cache.InMemory;

/// <summary>默认 dev / unit test 实现，进程内 <see cref="ConcurrentDictionary{TKey,TValue}"/>，无外部依赖。</summary>
internal sealed class InMemoryCacheProvider(TimeProvider clock) : ICacheProvider
{
    private sealed record class Entry(object? Value, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, Entry> _store = new();
    private readonly ConcurrentDictionary<string, byte> _locks = new();

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (this._store.TryGetValue(key, out Entry? entry))
        {
            if (entry.ExpiresAt > clock.GetUtcNow())
            {
                return Task.FromResult((T?)entry.Value);
            }

            this._store.TryRemove(key, out _);
        }

        return Task.FromResult<T?>(default);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default)
    {
        this._store[key] = new Entry(value, clock.GetUtcNow() + options.AbsoluteExpirationRelativeToNow);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        this._store.TryRemove(key, out _);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        bool exists = this._store.TryGetValue(key, out Entry? entry) && entry.ExpiresAt > clock.GetUtcNow();

        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public Task<long> IncrementAsync(string key, long delta, CancellationToken ct = default)
    {
        long result = 0;

        this._store.AddOrUpdate(
            key,
            _ =>
            {
                result = delta;

                return new Entry(delta, clock.GetUtcNow().AddYears(100));
            },
            (_, existing) =>
            {
                long current = existing.ExpiresAt > clock.GetUtcNow() && existing.Value is long l ? l : 0;
                result = current + delta;

                return existing with { Value = result };
            });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> TryAcquireLockAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        bool acquired = this._locks.TryAdd(key, 0);

        return Task.FromResult(acquired);
    }

    /// <inheritdoc />
    public Task ReleaseLockAsync(string key, CancellationToken ct = default)
    {
        this._locks.TryRemove(key, out _);

        return Task.CompletedTask;
    }
}
