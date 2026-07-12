// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>历史会话侧栏列表投影 DTO，不含消息明细。</summary>
public sealed record class AgentSessionSummary
{
    /// <summary>
    /// 获取会话标识。
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// 获取会话标题。
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// 获取最近活动时间。
    /// </summary>
    public required DateTimeOffset LastActivityTime { get; init; }

    /// <summary>
    /// 获取会话创建时间。
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }
}
