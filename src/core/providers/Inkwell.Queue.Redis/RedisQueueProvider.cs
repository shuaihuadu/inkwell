// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Inkwell.Queue.Redis;

/// <summary>
/// 基于 <see cref="StackExchange.Redis"/> Streams 的 <see cref="IQueueProvider"/> 实现（ADR-018，integration
/// test / prod 默认）。消费组名固定为 <c>inkwell-consumers</c>，消费者名取本进程 <see cref="Environment.MachineName"/> +
/// 进程 Id，允许同一队列多副本消费而不重复处理。
/// </summary>
/// <remarks>
/// 已知范围限制（未实现）：不做基于 <c>XCLAIM</c> 的"消费者崩溃后自动认领"后台扫描——那需要一个常驻
/// <c>BackgroundService</c> 定期跑 <c>XPENDING</c> 认领超时条目，本 Provider 类本身不常驻、不适合内建定时器；
/// DLQ 24 小时保留（<see cref="QueueOptions.DlqRetentionHours"/>）同理不做自动过期清理，两者都需要配套的
/// 后台清理任务，留待 <c>Inkwell.Worker</c> 落地对应 HD 时一并提供。
/// </remarks>
internal sealed class RedisQueueProvider(IConnectionMultiplexer connection, IOptions<QueueOptions> options) : IQueueProvider
{
    private const string ConsumerGroup = "inkwell-consumers";

    private static readonly string consumerName = $"{Environment.MachineName}:{Environment.ProcessId}";

    /// <summary>获取当前连接对应的 Redis 逻辑数据库。</summary>
    private IDatabase Database => connection.GetDatabase();

    /// <inheritdoc />
    public async Task EnqueueAsync<T>(string queueName, MessageEnvelope<T> message, CancellationToken ct = default)
    {
        NameValueEntry[] fields = ToFields(message);

        await this.Database.StreamAddAsync(queueName, fields).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MessageEnvelope<T>> DequeueAsync<T>(string queueName, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await this.EnsureConsumerGroupAsync(queueName).ConfigureAwait(false);

        while (!ct.IsCancellationRequested)
        {
            StreamEntry[] entries = await this.Database.StreamReadGroupAsync(
                queueName, ConsumerGroup, consumerName, ">", count: 10).ConfigureAwait(false);

            if (entries.Length == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);

                continue;
            }

            foreach (StreamEntry entry in entries)
            {
                yield return ToEnvelope<T>(entry);
            }
        }
    }

    /// <inheritdoc />
    public async Task AcknowledgeAsync(string queueName, string messageId, CancellationToken ct = default)
    {
        await this.Database.StreamAcknowledgeAsync(queueName, ConsumerGroup, messageId).ConfigureAwait(false);
        await this.Database.StreamDeleteAsync(queueName, [messageId]).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// 未达 <see cref="QueueOptions.MaxDeliveryAttempts"/> 时递增 <c>DeliveryCount</c> 后重新入队；
    /// 达到上限则转入 <c>{queueName}:dlq</c> 死信流。两种情况都先确认 + 删除原条目，避免重复投递。
    /// </remarks>
    public async Task NegativeAcknowledgeAsync(string queueName, string messageId, CancellationToken ct = default)
    {
        StreamEntry[] range = await this.Database.StreamRangeAsync(queueName, messageId, messageId).ConfigureAwait(false);

        if (range.Length == 0)
        {
            return;
        }

        StreamEntry entry = range[0];
        int deliveryCount = ReadInt32Field(entry, "deliveryCount") + 1;

        NameValueEntry[] fields = [
            new NameValueEntry("payload", ReadStringField(entry, "payload")),
            new NameValueEntry("messageId", ReadStringField(entry, "messageId")),
            new NameValueEntry("enqueuedTime", ReadStringField(entry, "enqueuedTime")),
            new NameValueEntry("deliveryCount", deliveryCount),
            new NameValueEntry("traceParent", ReadStringField(entry, "traceParent")),
        ];

        string targetStream = deliveryCount >= options.Value.MaxDeliveryAttempts ? $"{queueName}:dlq" : queueName;

        await this.Database.StreamAddAsync(targetStream, fields).ConfigureAwait(false);
        await this.Database.StreamAcknowledgeAsync(queueName, ConsumerGroup, messageId).ConfigureAwait(false);
        await this.Database.StreamDeleteAsync(queueName, [messageId]).ConfigureAwait(false);
    }

    /// <summary>确保目标队列对应的消费组已存在；若已被其他副本创建（<c>BUSYGROUP</c>）则忽略。</summary>
    /// <param name="queueName">队列（Redis Stream）名称。</param>
    private async Task EnsureConsumerGroupAsync(string queueName)
    {
        try
        {
            // 用 StreamPosition.Beginning（"0"）而非 NewMessages（"$"）创建消费组：后者只投递消费组创建
            // 之后新写入的条目，如果生产者先于消费组创建就已经往流里写了消息（例如 WebApi 先起、Worker 后起），
            // 这些消息会被永久跳过、永远不会投递给任何消费者。从头开始能保证消费组创建时刻流里已有的全部未消费
            // 条目都会被投递，符合"至少投递一次"的队列语义。
            await this.Database.StreamCreateConsumerGroupAsync(queueName, ConsumerGroup, StreamPosition.Beginning, createStream: true).ConfigureAwait(false);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase))
        {
            // 消费组已存在（同队列多副本竞相创建），忽略。
        }
    }

    /// <summary>把 <see cref="MessageEnvelope{T}"/> 序列化为 Redis Stream 的字段数组。</summary>
    /// <param name="message">待入队的消息信封。</param>
    /// <returns>供 <c>XADD</c> 使用的字段数组。</returns>
    private static NameValueEntry[] ToFields<T>(MessageEnvelope<T> message) =>
    [
        new NameValueEntry("payload", JsonSerializer.Serialize(message.Payload)),
        new NameValueEntry("messageId", message.MessageId),
        new NameValueEntry("enqueuedTime", message.EnqueuedTime.ToString("O")),
        new NameValueEntry("deliveryCount", message.DeliveryCount),
        new NameValueEntry("traceParent", message.TraceParent ?? string.Empty),
    ];

    /// <summary>
    /// 把 Redis Stream 条目还原为 <see cref="MessageEnvelope{T}"/>。
    /// <see cref="MessageEnvelope{T}.MessageId"/> 在这里被覆盖为 Redis Stream 原生条目 ID（而不是入队时
    /// 业务侧设置的值）——<see cref="AcknowledgeAsync"/>/<see cref="NegativeAcknowledgeAsync"/> 需要用原生
    /// 条目 ID 才能对应到 Stream 的 Pending Entry List，业务侧原始 MessageId 仍完整保留在 <c>messageId</c>
    /// 字段里，只是不再作为对外可见的 <see cref="MessageEnvelope{T}.MessageId"/>。
    /// </summary>
    /// <param name="entry">原生 Redis Stream 条目。</param>
    /// <returns>还原出的消息信封。</returns>
    private static MessageEnvelope<T> ToEnvelope<T>(StreamEntry entry)
    {
        T? payload = JsonSerializer.Deserialize<T>(ReadStringField(entry, "payload"));
        DateTimeOffset enqueuedTime = DateTimeOffset.Parse(ReadStringField(entry, "enqueuedTime"));
        int deliveryCount = ReadInt32Field(entry, "deliveryCount");
        string traceParent = ReadStringField(entry, "traceParent");

        return new MessageEnvelope<T>(
            entry.Id!,
            payload!,
            enqueuedTime,
            deliveryCount,
            traceParent.Length == 0 ? null : traceParent);
    }

    /// <summary>读取 Stream 条目里指定字段的字符串值；字段不存在时返回空字符串。</summary>
    /// <param name="entry">Stream 条目。</param>
    /// <param name="fieldName">字段名。</param>
    /// <returns>字段值；不存在时为空字符串。</returns>
    private static string ReadStringField(StreamEntry entry, string fieldName) =>
        entry.Values.FirstOrDefault(v => v.Name == fieldName).Value.ToString();

    /// <summary>读取 Stream 条目里指定字段并解析为 <see cref="int"/>；解析失败时返回 0。</summary>
    /// <param name="entry">Stream 条目。</param>
    /// <param name="fieldName">字段名。</param>
    /// <returns>解析出的整数；失败时为 0。</returns>
    private static int ReadInt32Field(StreamEntry entry, string fieldName)
    {
        string raw = ReadStringField(entry, fieldName);

        return int.TryParse(raw, out int value) ? value : 0;
    }
}
