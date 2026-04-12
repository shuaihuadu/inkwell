using System.Collections.Concurrent;

namespace Inkwell;

/// <summary>
/// 基于内存的发布/订阅提供程序
/// 使用 ConcurrentDictionary + 锁保护的委托列表实现，适用于单进程开发调试场景
/// </summary>
/// <typeparam name="T">消息类型</typeparam>
public sealed class InMemoryPubSubProvider<T> : IPubSubProvider<T> where T : class
{
    private readonly ConcurrentDictionary<string, List<Func<T, CancellationToken, Task>>> _subscriptions = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public async Task PublishAsync(T message, string channel, CancellationToken cancellationToken = default)
    {
        if (!this._subscriptions.TryGetValue(channel, out List<Func<T, CancellationToken, Task>>? handlers))
        {
            return;
        }

        // [H4 修复] 在同一把锁内获取快照，避免 TryGetValue 和 snapshot 之间的竞态
        Func<T, CancellationToken, Task>[] snapshot;
        lock (this._lock)
        {
            snapshot = [.. handlers];
        }

        foreach (Func<T, CancellationToken, Task> handler in snapshot)
        {
            await handler(message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task<IAsyncDisposable> SubscribeAsync(string channel, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
    {
        lock (this._lock)
        {
            List<Func<T, CancellationToken, Task>> handlers = this._subscriptions.GetOrAdd(channel, _ => []);
            handlers.Add(handler);
        }

        IAsyncDisposable subscription = new Subscription(this, channel, handler);
        return Task.FromResult(subscription);
    }

    private void Unsubscribe(string channel, Func<T, CancellationToken, Task> handler)
    {
        if (this._subscriptions.TryGetValue(channel, out List<Func<T, CancellationToken, Task>>? handlers))
        {
            lock (this._lock)
            {
                handlers.Remove(handler);
            }
        }
    }

    /// <summary>
    /// 订阅令牌，用于取消订阅
    /// </summary>
    private sealed class Subscription(InMemoryPubSubProvider<T> provider, string channel, Func<T, CancellationToken, Task> handler) : IAsyncDisposable
    {
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            provider.Unsubscribe(channel, handler);
            return ValueTask.CompletedTask;
        }
    }
}
