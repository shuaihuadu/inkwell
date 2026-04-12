namespace Inkwell;

/// <summary>
/// 分析报告记录（数据载体）
/// </summary>
public sealed class AnalysisRecord
{
    /// <summary>
    /// 获取或设置记录唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置关联流水线运行 ID
    /// </summary>
    public string PipelineRunId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置主题
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置市场趋势分析
    /// </summary>
    public string MarketTrends { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目标受众
    /// </summary>
    public string TargetAudience { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置内容角度
    /// </summary>
    public string ContentAngles { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
