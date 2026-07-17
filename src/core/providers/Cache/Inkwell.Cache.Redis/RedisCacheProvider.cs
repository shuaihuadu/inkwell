// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Inkwell.Cache.Redis;

/// <summary>基于 <see cref="StackExchange.Redis"/> 的 <see cref="ICacheProvider"/> 实现（ADR-016，prod 默认）。</summary>
internal sealed class RedisCacheProvider(IConnectionMultiplexer connection, IOptions<CacheOptions> options) : ICacheProvider
{
    private static readonly RedisValue lockValue = "locked";

    /// <summary>获取当前连接对应的 Redis 逻辑数据库。</summary>
    private IDatabase Database => connection.GetDatabase();

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        RedisValue value = await this.Database.StringGetAsync(key).ConfigureAwait(false);

        if (!value.HasValue)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>((string)value!);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, CacheEntryOptions entryOptions, CancellationToken ct = default)
    {
        string json = JsonSerializer.Serialize(value);
        TimeSpan ttl = ClampTtl(entryOptions.AbsoluteExpirationRelativeToNow, options.Value);

        await this.Database.StringSetAsync(key, json, ttl).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct = default) =>
        await this.Database.KeyDeleteAsync(key).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
        await this.Database.KeyExistsAsync(key).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<long> IncrementAsync(string key, long delta, CancellationToken ct = default) =>
        await this.Database.StringIncrementAsync(key, delta).ConfigureAwait(false);

    /// <inheritdoc />
    /// <remarks>
    /// 用固定哨兵值实现 <c>SET NX</c> 语义（<see cref="ICacheProvider"/> 端口签名不携带 owner token，
    /// 与 <c>InMemoryCacheProvider</c> 的简化程度一致）。
    /// </remarks>
    public async Task<bool> TryAcquireLockAsync(string key, TimeSpan ttl, CancellationToken ct = default) =>
        await this.Database.LockTakeAsync(key, lockValue, ttl).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task ReleaseLockAsync(string key, CancellationToken ct = default) =>
        await this.Database.LockReleaseAsync(key, lockValue).ConfigureAwait(false);

    /// <summary>
    /// 将请求的 TTL 夹紧到 <paramref name="cacheOptions"/> 配置的 <c>MinTtlSeconds</c>/<c>MaxTtlSeconds</c> 区间内。
    /// </summary>
    /// <param name="requested">调用方通过 <see cref="CacheEntryOptions"/> 请求的过期时间。</param>
    /// <param name="cacheOptions">当前 Provider 的 <see cref="CacheOptions"/> 配置。</param>
    /// <returns>夹紧后的 TTL。</returns>
    private static TimeSpan ClampTtl(TimeSpan requested, CacheOptions cacheOptions)
    {
        TimeSpan min = TimeSpan.FromSeconds(cacheOptions.MinTtlSeconds);
        TimeSpan max = TimeSpan.FromSeconds(cacheOptions.MaxTtlSeconds);

        if (requested < min)
        {
            return min;
        }

        return requested > max ? max : requested;
    }
}
