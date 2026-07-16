// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示用户与固定 Agent 版本之间的产品会话。
/// </summary>
public sealed record class AgentConversation : IHasOwner, IHasTimestamps
{
    /// <summary>获取会话标识。</summary>
    public required Guid Id { get; init; }

    /// <summary>获取供 Session Store 使用的不透明会话键。</summary>
    public required string SessionKey { get; init; }

    /// <summary>获取会话所属 Agent 标识。</summary>
    public required Guid AgentId { get; init; }

    /// <summary>获取创建会话时锁定的 Agent 版本标识。</summary>
    public required Guid AgentVersionId { get; init; }

    /// <summary>获取会话所属参与用户标识。</summary>
    public required Guid OwnerUserId { get; init; }

    /// <summary>获取会话标题。</summary>
    public string? Title { get; init; }

    /// <summary>获取最后成功提交消息批次的服务端执行标识。</summary>
    public string? LastCommittedRunId { get; init; }

    /// <summary>获取最后一次消息活动时间。</summary>
    public required DateTimeOffset LastActivityTime { get; init; }

    /// <summary>获取创建时间。</summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>获取更新时间。</summary>
    public required DateTimeOffset UpdatedTime { get; init; }

}
