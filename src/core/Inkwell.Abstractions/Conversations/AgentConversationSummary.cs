namespace Inkwell;

/// <summary>历史会话侧栏列表投影 DTO，不含消息明细。</summary>
public sealed record class AgentConversationSummary
{
    public required Guid Id { get; init; }

    public string? Title { get; init; }

    public required DateTimeOffset LastActivityTime { get; init; }

    public required DateTimeOffset CreatedTime { get; init; }
}
