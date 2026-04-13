namespace Inkwell;

/// <summary>
/// 流水线运行记录持久化提供程序
/// </summary>
public interface IPipelineRunPersistenceProvider : IPersistenceProvider<PipelineRunRecord, string>
{
    /// <summary>
    /// 获取最近的运行记录
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近的运行记录集合</returns>
    Task<IReadOnlyList<PipelineRunRecord>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
