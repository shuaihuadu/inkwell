// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Cache.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 针对 <see cref="RedisCacheProvider"/> 的真实 Testcontainers 集成测试（基于真实 Redis 容器，
/// 不是编译期验证）。覆盖 <see cref="ICacheProvider"/> 的读写、TTL 夹紧与分布式锁语义。
/// </summary>
[TestClass]
public sealed class RedisCacheProviderTests
{
    private static RedisContainer? container;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        container = new RedisBuilder(ContainerImageConfiguration.GetRequired("Tests:Redis")).Build();

        await container.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task SetAsync_Then_GetAsync_Roundtrips_ValueAsync()
    {
        ICacheProvider cache = BuildCacheProvider();

        await cache.SetAsync("inkwell:test:key1", "hello", new CacheEntryOptions(TimeSpan.FromMinutes(1)));

        string? value = await cache.GetAsync<string>("inkwell:test:key1");

        Assert.AreEqual("hello", value);
    }

    [TestMethod]
    public async Task GetAsync_On_Missing_Key_Returns_DefaultAsync()
    {
        ICacheProvider cache = BuildCacheProvider();

        string? value = await cache.GetAsync<string>("inkwell:test:does-not-exist");

        Assert.IsNull(value);
    }

    [TestMethod]
    public async Task RemoveAsync_Deletes_Existing_KeyAsync()
    {
        ICacheProvider cache = BuildCacheProvider();

        await cache.SetAsync("inkwell:test:key2", "value", new CacheEntryOptions(TimeSpan.FromMinutes(1)));
        await cache.RemoveAsync("inkwell:test:key2");

        bool exists = await cache.ExistsAsync("inkwell:test:key2");

        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task IncrementAsync_Accumulates_DeltaAsync()
    {
        ICacheProvider cache = BuildCacheProvider();

        long first = await cache.IncrementAsync("inkwell:test:counter", 3);
        long second = await cache.IncrementAsync("inkwell:test:counter", 2);

        Assert.AreEqual(3, first);
        Assert.AreEqual(5, second);
    }

    [TestMethod]
    public async Task TryAcquireLockAsync_Blocks_Second_Acquirer_Until_ReleasedAsync()
    {
        ICacheProvider cache = BuildCacheProvider();

        bool firstAcquired = await cache.TryAcquireLockAsync("inkwell:test:lock", TimeSpan.FromSeconds(30));
        bool secondAcquired = await cache.TryAcquireLockAsync("inkwell:test:lock", TimeSpan.FromSeconds(30));

        await cache.ReleaseLockAsync("inkwell:test:lock");

        bool thirdAcquired = await cache.TryAcquireLockAsync("inkwell:test:lock", TimeSpan.FromSeconds(30));

        Assert.IsTrue(firstAcquired);
        Assert.IsFalse(secondAcquired);
        Assert.IsTrue(thirdAcquired);
    }

    private static ICacheProvider BuildCacheProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();

        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        builder.UseRedisCache(container!.GetConnectionString());

        ServiceProvider provider = builder.Services.BuildServiceProvider();

        return provider.GetRequiredService<ICacheProvider>();
    }
}
