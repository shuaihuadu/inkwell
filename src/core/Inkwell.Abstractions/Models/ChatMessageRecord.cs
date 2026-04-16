namespace Inkwell;

/// <summary>
/// 聊天消息记录
/// </summary>
/// <param name="Id">消息 ID</param>
/// <param name="Role">角色（user / assistant / system）</param>
/// <param name="Content">消息内容</param>
/// <param name="Status">状态（done / error）</param>
/// <param name="CreatedAt">创建时间</param>
public sealed record ChatMessageRecord(
    string Id,
    string Role,
    string Content,
    string Status,
    DateTimeOffset CreatedAt);
