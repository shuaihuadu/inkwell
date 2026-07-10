using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Inkwell;

namespace Inkwell.Queue.Channels;

/// <summary>
/// 默认 dev / unit test 实现，基于 <see cref="System.Threading.Channels"/>，进程内、无持久化。
/// Ack/NAck 为 no-op（Channels 不追踪在途消息，属 dev-only 已知简化，integration/prod 走 RedisStreamQueueProvider）。
/// </summary>
internal sealed class ChannelsQueueProvider : IQueueProvider
{
    private readonly ConcurrentDictionary<string, Channel<object>> _channels = new();

    /// <inheritdoc />
    public Task EnqueueAsync<T>(string queueName, MessageEnvelope<T> message, CancellationToken ct = default)
    {
        Channel<object> channel = this.GetChannel(queueName);

        return channel.Writer.WriteAsync(message, ct).AsTask();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MessageEnvelope<T>> DequeueAsync<T>(string queueName, [EnumeratorCancellation] CancellationToken ct = default)
    {
        Channel<object> channel = this.GetChannel(queueName);

        await foreach (object? item in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            if (item is MessageEnvelope<T> envelope)
            {
                yield return envelope;
            }
        }
    }

    /// <inheritdoc />
    public Task AcknowledgeAsync(string queueName, string messageId, CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task NegativeAcknowledgeAsync(string queueName, string messageId, CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>获取（或按需创建）指定队列名对应的进程内 <see cref="Channel{T}"/>。</summary>
    /// <param name="queueName">队列名称。</param>
    /// <returns>对应的 Channel 实例。</returns>
    private Channel<object> GetChannel(string queueName) =>
        this._channels.GetOrAdd(queueName, static _ => Channel.CreateUnbounded<object>());
}
