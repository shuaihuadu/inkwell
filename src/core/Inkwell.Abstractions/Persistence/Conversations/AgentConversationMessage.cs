
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>会话内单条消息的持久化业务 Model；跨用户 / Agent 消息统一存储（NFR-005）。</summary>
public sealed record class AgentConversationMessage : IHasTimestamps
{
    public required Guid Id { get; init; }

    public required Guid ConversationId { get; init; }

    public required ChatRole Role { get; init; }

    /// <summary><see cref="AIContent"/> 列表的序列化存储。</summary>
    public required string ContentJson { get; init; }

    public string? AuthorName { get; init; }

    /// <summary>会话内严格递增，用于确定性排序（不仅依赖 CreatedTime）。</summary>
    public required int SequenceNumber { get; init; }

    public required DateTimeOffset CreatedTime { get; init; }

    public required DateTimeOffset UpdatedTime { get; init; }
}
