namespace Inkwell;

/// <summary>
/// 队列提供程序接口
/// 支持 FIFO 队列（Enqueue/Dequeue）操作
/// </summary>
/// <typeparam name="T">消息类型</typeparam>
public interface IQueueProvider<T> where T : class
{
    /// <summary>
    /// 将项目加入队列
    /// </summary>
    /// <param name="item">要入队的项目</param>
    /// <param name="queueName">队列名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    Task EnqueueAsync(T item, string? queueName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从队列中取出一个项目
    /// </summary>
    /// <param name="queueName">队列名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>队列中的项目，队列为空时返回 null</returns>
    Task<T?> DequeueAsync(string? queueName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取队列中的项目数量
    /// </summary>
    /// <param name="queueName">队列名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>队列中的项目数量</returns>
    Task<long> GetCountAsync(string? queueName = null, CancellationToken cancellationToken = default);
}
