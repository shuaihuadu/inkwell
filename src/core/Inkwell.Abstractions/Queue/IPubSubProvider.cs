namespace Inkwell;

/// <summary>
/// 发布/订阅提供程序接口
/// 支持消息的发布和订阅操作
/// </summary>
/// <typeparam name="T">消息类型</typeparam>
public interface IPubSubProvider<T> where T : class
{
    /// <summary>
    /// 发布消息到指定频道
    /// </summary>
    /// <param name="message">要发布的消息</param>
    /// <param name="channel">频道名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    Task PublishAsync(T message, string channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅指定频道的消息
    /// </summary>
    /// <param name="channel">频道名称</param>
    /// <param name="handler">消息处理委托</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>订阅令牌，用于取消订阅</returns>
    Task<IAsyncDisposable> SubscribeAsync(string channel, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
}
