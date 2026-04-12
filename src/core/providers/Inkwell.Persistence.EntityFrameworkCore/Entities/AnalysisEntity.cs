using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkwell.Persistence.EntityFrameworkCore.Entities;

/// <summary>
/// 分析报告实体
/// </summary>
[Table("Analyses")]
public sealed class AnalysisEntity
{
    /// <summary>
    /// 获取或设置唯一标识
    /// </summary>
    [Key]
    [MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置关联流水线运行 ID
    /// </summary>
    [Required]
    [MaxLength(32)]
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
