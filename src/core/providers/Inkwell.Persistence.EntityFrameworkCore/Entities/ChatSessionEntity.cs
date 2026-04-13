using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkwell.Persistence.EntityFrameworkCore.Entities;

/// <summary>
/// 聊天会话实体
/// </summary>
[Table("ChatSessions")]
public sealed class ChatSessionEntity
{
    /// <summary>
    /// 获取或设置会话唯一标识（= threadId）
    /// </summary>
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置关联的 Agent ID
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置会话标题（自动从首条用户消息截取）
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// 获取或设置 MAF AgentSession 序列化状态（JSON）
    /// </summary>
    [Required]
    public string SessionState { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置消息总数
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// 获取或设置创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 获取或设置更新时间
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
