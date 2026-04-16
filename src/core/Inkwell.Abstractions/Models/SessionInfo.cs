namespace Inkwell;

/// <summary>
/// 会话信息
/// </summary>
/// <param name="Id">会话 ID</param>
/// <param name="AgentId">Agent ID</param>
/// <param name="Title">会话标题</param>
/// <param name="MessageCount">消息总数</param>
/// <param name="CreatedAt">创建时间</param>
/// <param name="UpdatedAt">更新时间</param>
public sealed record SessionInfo(
    string Id,
    string AgentId,
    string? Title,
    int MessageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
