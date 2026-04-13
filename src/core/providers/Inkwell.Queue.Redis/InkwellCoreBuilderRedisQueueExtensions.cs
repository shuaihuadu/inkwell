using Inkwell;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Inkwell.Queue.Redis;

/// <summary>
/// Redis 队列服务注册扩展方法
/// </summary>
public static class InkwellCoreBuilderRedisQueueExtensions
{
    /// <summary>
    /// 使用 Redis 作为队列和发布/订阅后端
    /// </summary>
    /// <param name="builder">Inkwell 核心构建器</param>
    /// <param name="connectionString">Redis 连接字符串</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseRedisQueue(this InkwellCoreBuilder builder, string connectionString)
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(connectionString));

        builder.Services.AddSingleton(typeof(IQueueProvider<>), typeof(RedisQueueProvider<>));
        builder.Services.AddSingleton(typeof(IPubSubProvider<>), typeof(RedisPubSubProvider<>));

        return builder;
    }
}
