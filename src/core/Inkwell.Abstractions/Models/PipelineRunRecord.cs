namespace Inkwell;

/// <summary>
/// 流水线运行记录（数据载体）
/// </summary>
public sealed class PipelineRunRecord
{
    /// <summary>
    /// 获取或设置运行唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文章主题
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置运行状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置关联文章 ID
    /// </summary>
    public string? ArticleId { get; set; }

    /// <summary>
    /// 获取或设置开始时间
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// 获取或设置结束时间
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// 获取或设置总修订次数
    /// </summary>
    public int TotalRevisions { get; set; }
}
