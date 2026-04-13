using System.Text.Json;
using Inkwell;
using StackExchange.Redis;

namespace Inkwell.Queue.Redis;

/// <summary>
/// 基于 Redis 的队列提供程序
/// 使用 Redis List 实现 FIFO 队列
/// </summary>
/// <typeparam name="T">消息类型</typeparam>
public sealed class RedisQueueProvider<T>(IConnectionMultiplexer connection) : IQueueProvider<T> where T : class
{
    private IDatabase Database => connection.GetDatabase();

    private static string GetQueueKey(string? queueName) => $"inkwell:queue:{queueName ?? "default"}";

    /// <inheritdoc />
    public async Task EnqueueAsync(T item, string? queueName = null, CancellationToken cancellationToken = default)
    {
        string json = JsonSerializer.Serialize(item);
        await this.Database.ListRightPushAsync(GetQueueKey(queueName), json).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T?> DequeueAsync(string? queueName = null, CancellationToken cancellationToken = default)
    {
        RedisValue value = await this.Database.ListLeftPopAsync(GetQueueKey(queueName)).ConfigureAwait(false);
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    /// <inheritdoc />
    public async Task<long> GetCountAsync(string? queueName = null, CancellationToken cancellationToken = default)
    {
        return await this.Database.ListLengthAsync(GetQueueKey(queueName)).ConfigureAwait(false);
    }
}
