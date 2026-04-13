using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkwell.Persistence.EntityFrameworkCore.Entities;

/// <summary>
/// 聊天消息实体（用于前端展示，不参与 Agent 运行时）
/// </summary>
[Table("ChatMessages")]
public sealed class ChatMessageEntity
{
    /// <summary>
    /// 获取或设置消息唯一标识
    /// </summary>
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置所属会话 ID（FK -> ChatSessionEntity.Id）
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置消息角色（user / assistant / system）
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置消息内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置消息状态（done / error）
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "done";

    /// <summary>
    /// 获取或设置创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
