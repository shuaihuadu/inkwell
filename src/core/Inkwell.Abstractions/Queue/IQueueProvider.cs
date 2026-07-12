// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 队列端口 facade。环境对称双 Provider：Channels（dev/unit test）/ RedisStream（integration/prod，ADR-018）。
/// </summary>
public interface IQueueProvider
{
    /// <summary>将消息加入指定队列。</summary>
    /// <typeparam name="T">消息负载类型。</typeparam>
    /// <param name="queueName">队列名称。</param>
    /// <param name="message">消息信封。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task EnqueueAsync<T>(string queueName, MessageEnvelope<T> message, CancellationToken ct = default);

    /// <summary>异步读取指定队列的消息。</summary>
    /// <typeparam name="T">消息负载类型。</typeparam>
    /// <param name="queueName">队列名称。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>消息信封的异步序列。</returns>
    IAsyncEnumerable<MessageEnvelope<T>> DequeueAsync<T>(string queueName, CancellationToken ct = default);

    /// <summary>确认指定消息已成功处理。</summary>
    /// <param name="queueName">队列名称。</param>
    /// <param name="messageId">消息标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task AcknowledgeAsync(string queueName, string messageId, CancellationToken ct = default);

    /// <summary>否定确认指定消息，以便按队列策略重新投递或转入死信队列。</summary>
    /// <param name="queueName">队列名称。</param>
    /// <param name="messageId">消息标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task NegativeAcknowledgeAsync(string queueName, string messageId, CancellationToken ct = default);
}
