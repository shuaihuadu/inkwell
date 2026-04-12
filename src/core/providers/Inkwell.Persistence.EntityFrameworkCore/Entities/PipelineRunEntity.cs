using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkwell.Persistence.EntityFrameworkCore.Entities;

/// <summary>
/// 流水线运行记录实体
/// </summary>
[Table("PipelineRuns")]
public sealed class PipelineRunEntity
{
    /// <summary>
    /// 获取或设置唯一标识
    /// </summary>
    [Key]
    [MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文章主题
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置运行状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置关联文章 ID
    /// </summary>
    [MaxLength(32)]
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
