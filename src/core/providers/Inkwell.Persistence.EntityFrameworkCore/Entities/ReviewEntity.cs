using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkwell.Persistence.EntityFrameworkCore.Entities;

/// <summary>
/// 审核记录实体
/// </summary>
[Table("Reviews")]
public sealed class ReviewEntity
{
    /// <summary>
    /// 获取或设置唯一标识
    /// </summary>
    [Key]
    [MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置关联文章 ID
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string ArticleId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置审核轮次
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// 获取或设置审核者类型（AI / Human）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ReviewerType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置是否通过
    /// </summary>
    public bool Approved { get; set; }

    /// <summary>
    /// 获取或设置审核反馈
    /// </summary>
    public string Feedback { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置评分
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 获取或设置审核时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
