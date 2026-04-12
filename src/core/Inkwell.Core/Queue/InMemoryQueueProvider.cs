using System.Collections.Concurrent;

namespace Inkwell;

/// <summary>
/// 基于内存的队列提供程序
/// 使用 ConcurrentQueue 实现，适用于单进程开发调试场景
/// </summary>
/// <typeparam name="T">消息类型</typeparam>
public sealed class InMemoryQueueProvider<T> : IQueueProvider<T> where T : class
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<T>> _queues = new();

    private ConcurrentQueue<T> GetQueue(string? queueName)
    {
        string key = queueName ?? "default";
        return this._queues.GetOrAdd(key, _ => new ConcurrentQueue<T>());
    }

    /// <inheritdoc />
    public Task EnqueueAsync(T item, string? queueName = null, CancellationToken cancellationToken = default)
    {
        this.GetQueue(queueName).Enqueue(item);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<T?> DequeueAsync(string? queueName = null, CancellationToken cancellationToken = default)
    {
        this.GetQueue(queueName).TryDequeue(out T? item);
        return Task.FromResult(item);
    }

    /// <inheritdoc />
    public Task<long> GetCountAsync(string? queueName = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)this.GetQueue(queueName).Count);
    }
}
