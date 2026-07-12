// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 会话内单条消息的持久化业务 Model；完整保存 MAF 使用的 <see cref="ChatMessage"/>，
/// 避免丢失工具调用、工具结果、消息标识与扩展属性。
/// </summary>
public sealed record class AgentChatMessage : IHasTimestamps
{
    /// <summary>获取持久化消息标识。</summary>
    public required Guid Id { get; init; }

    /// <summary>获取所属业务会话标识。</summary>
    public required Guid SessionId { get; init; }

    /// <summary>获取完整的 MAF Chat Message。</summary>
    public required ChatMessage Message { get; init; }

    /// <summary>获取会话内严格递增的消息序号。</summary>
    public required int SequenceNumber { get; init; }

    /// <summary>获取创建时间。</summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>获取更新时间。</summary>
    public required DateTimeOffset UpdatedTime { get; init; }
}
