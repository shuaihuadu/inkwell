// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.AI;

namespace Inkwell.WebApi.Conversations;

/// <summary>表示产品会话消息响应。</summary>
public sealed record class AgentChatMessageResponse
{
    /// <summary>获取持久化消息标识。</summary>
    public required Guid Id { get; init; }

    /// <summary>获取完整聊天消息。</summary>
    public required ChatMessage Message { get; init; }

    /// <summary>获取会话内消息序号。</summary>
    public required int SequenceNumber { get; init; }

    /// <summary>获取创建时间。</summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>获取更新时间。</summary>
    public required DateTimeOffset UpdatedTime { get; init; }
}