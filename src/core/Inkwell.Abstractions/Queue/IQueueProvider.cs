namespace Inkwell;

/// <summary>
/// 队列端口 facade。环境对称双 Provider：Channels（dev/unit test）/ RedisStream（integration/prod，ADR-018）。
/// </summary>
public interface IQueueProvider
{
    Task EnqueueAsync<T>(string queueName, MessageEnvelope<T> message, CancellationToken ct = default);

    IAsyncEnumerable<MessageEnvelope<T>> DequeueAsync<T>(string queueName, CancellationToken ct = default);

    Task AcknowledgeAsync(string queueName, string messageId, CancellationToken ct = default);

    Task NegativeAcknowledgeAsync(string queueName, string messageId, CancellationToken ct = default);
}
