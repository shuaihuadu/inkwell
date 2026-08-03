// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示产品会话列表中的裁剪查询项。
/// </summary>
public sealed record class AgentConversationListItem
{
    /// <summary>
    /// 获取会话标识。
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// 获取会话锁定的 Agent 版本标识。
    /// </summary>
    public required Guid AgentVersionId { get; init; }

    /// <summary>
    /// 获取会话标题。
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// 获取最后活动时间。
    /// </summary>
    public required DateTimeOffset LastActivityTime { get; init; }

    /// <summary>
    /// 获取创建时间。
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }
}