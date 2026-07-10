using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Inkwell;

namespace Inkwell.Queue.Redis;

/// <summary><see cref="IInkwellBuilder"/> 的 Redis Stream Queue Provider 唯一入口扩展（ADR-018）。</summary>
public static class InkwellQueueRedisServiceCollectionExtensions
{
    /// <summary>
    /// 注册基于 <see cref="StackExchange.Redis"/> Streams 的 <see cref="IQueueProvider"/> 实现。
    /// </summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <param name="connectionString">Redis 连接字符串。</param>
    /// <param name="configure">可选的 <see cref="QueueOptions"/> 编程式追加配置。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseRedisQueue(this IInkwellBuilder builder, string connectionString, Action<QueueOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        OptionsBuilder<QueueOptions> optionsBuilder = builder.Services.AddOptions<QueueOptions>()
            .BindConfiguration("Inkwell:Queue")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        // TryAddSingleton：若 Inkwell.Cache.Redis 已注册过 IConnectionMultiplexer（同一 Redis 部署场景），
        // 复用同一个 multiplexer——StackExchange.Redis 的 multiplexer 本身设计为可安全复用/多路复用。
        builder.Services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
        builder.Services.AddSingleton<IQueueProvider, RedisQueueProvider>();

        return builder;
    }
}
