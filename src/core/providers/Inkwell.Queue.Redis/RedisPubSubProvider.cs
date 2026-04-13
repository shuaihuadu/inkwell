using System.Text.Json;
using Inkwell;
using StackExchange.Redis;

namespace Inkwell.Queue.Redis;

/// <summary>
/// 基于 Redis 的发布/订阅提供程序
/// 使用 Redis Pub/Sub 实现
/// </summary>
/// <typeparam name="T">消息类型</typeparam>
public sealed class RedisPubSubProvider<T>(IConnectionMultiplexer connection) : IPubSubProvider<T> where T : class
{
    private ISubscriber Subscriber => connection.GetSubscriber();

    private static string GetChannelKey(string channel) => $"inkwell:pubsub:{channel}";

    /// <inheritdoc />
    public async Task PublishAsync(T message, string channel, CancellationToken cancellationToken = default)
    {
        string json = JsonSerializer.Serialize(message);
        await this.Subscriber.PublishAsync(RedisChannel.Literal(GetChannelKey(channel)), json).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IAsyncDisposable> SubscribeAsync(string channel, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
    {
        ChannelMessageQueue messageQueue = await this.Subscriber
            .SubscribeAsync(RedisChannel.Literal(GetChannelKey(channel)))
            .ConfigureAwait(false);

        messageQueue.OnMessage(async channelMessage =>
        {
            T? message = JsonSerializer.Deserialize<T>(channelMessage.Message.ToString());
            if (message is not null)
            {
                await handler(message, CancellationToken.None).ConfigureAwait(false);
            }
        });

        return new Subscription(messageQueue);
    }

    /// <summary>
    /// Redis 订阅令牌
    /// </summary>
    private sealed class Subscription(ChannelMessageQueue messageQueue) : IAsyncDisposable
    {
        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await messageQueue.UnsubscribeAsync().ConfigureAwait(false);
        }
    }
}
