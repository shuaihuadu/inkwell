using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Inkwell;

namespace Inkwell.Cache.Redis;

/// <summary><see cref="IInkwellBuilder"/> 的 Redis Cache Provider 唯一入口扩展（ADR-016）。</summary>
public static class InkwellCacheRedisServiceCollectionExtensions
{
    /// <summary>
    /// 注册基于 <see cref="StackExchange.Redis"/> 的 <see cref="ICacheProvider"/> 实现。
    /// </summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <param name="connectionString">Redis 连接字符串。</param>
    /// <param name="configure">可选的 <see cref="CacheOptions"/> 编程式追加配置。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseRedisCache(this IInkwellBuilder builder, string connectionString, Action<CacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        OptionsBuilder<CacheOptions> optionsBuilder = builder.Services.AddOptions<CacheOptions>()
            .BindConfiguration("Inkwell:Cache")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        builder.Services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
        builder.Services.AddSingleton<ICacheProvider, RedisCacheProvider>();

        return builder;
    }
}
