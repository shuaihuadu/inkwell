// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>会话内单条消息的持久化业务 Model；跨用户 / Agent 消息统一存储（NFR-005）。</summary>
public sealed record class AgentChatMessage : IHasTimestamps
{
    public required Guid Id { get; init; }

    public required Guid SessionId { get; init; }

    public required ChatRole Role { get; init; }

    public required string ContentJson { get; init; }

    public string? Content { get; set; }

    public string? AuthorName { get; init; }

    public required int SequenceNumber { get; init; }

    public required DateTimeOffset CreatedTime { get; init; }

    public required DateTimeOffset UpdatedTime { get; init; }
}
