// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示产品会话在服务端持久化的 Agent Session 检查点。
/// </summary>
public sealed record class AgentSessionState : IHasTimestamps
{
    /// <summary>表示尚未写入任何会话状态时的空 JSON 内容。</summary>
    public const string Empty = "{}";

    /// <summary>获取会话状态标识。</summary>
    public required Guid Id { get; init; }

    /// <summary>获取所属产品会话标识。</summary>
    public required Guid ConversationId { get; init; }

    /// <summary>获取序列化后的 Session 状态 JSON 内容。</summary>
    public required string SessionState { get; init; }

    /// <summary>获取创建时间。</summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>获取更新时间。</summary>
    public required DateTimeOffset UpdatedTime { get; init; }
}
