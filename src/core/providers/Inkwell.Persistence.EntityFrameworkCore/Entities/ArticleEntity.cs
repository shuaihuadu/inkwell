using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkwell.Persistence.EntityFrameworkCore.Entities;

/// <summary>
/// 文章实体
/// </summary>
[Table("Articles")]
public sealed class ArticleEntity
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
    /// 获取或设置文章标题
    /// </summary>
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文章正文
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文章状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置修订版本号
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// 获取或设置创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 获取或设置更新时间
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
