using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkwell.Persistence.EntityFrameworkCore.Entities;

/// <summary>
/// 知识库文档实体
/// </summary>
[Table("KnowledgeDocuments")]
public sealed class KnowledgeDocumentEntity
{
    /// <summary>
    /// 获取或设置文档唯一标识
    /// </summary>
    [Key]
    [MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文档标题
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文档原始内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件类型（txt / md）
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string FileType { get; set; } = "txt";

    /// <summary>
    /// 获取或设置来源链接
    /// </summary>
    [MaxLength(500)]
    public string? SourceLink { get; set; }

    /// <summary>
    /// 获取或设置切片总数
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// 获取或设置创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// 知识库文档切片实体
/// </summary>
[Table("KnowledgeChunks")]
public sealed class KnowledgeChunkEntity
{
    /// <summary>
    /// 获取或设置切片唯一标识
    /// </summary>
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置所属文档 ID
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置切片序号（从 0 开始）
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// 获取或设置切片文本内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
