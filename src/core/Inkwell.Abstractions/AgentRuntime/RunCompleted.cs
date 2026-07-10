namespace Inkwell;

/// <summary>对应 AG-UI <c>lifecycle</c>；复用 <see cref="AgentTurnResult"/> 保证流式终态与非流式路径字段一致。</summary>
public sealed record class RunCompleted : AgentRunEvent
{
    public required AgentTurnResult Result { get; init; }
}
