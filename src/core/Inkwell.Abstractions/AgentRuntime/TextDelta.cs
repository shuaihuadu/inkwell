namespace Inkwell;

/// <summary>对应 AG-UI <c>message</c>。</summary>
public sealed record class TextDelta : AgentRunEvent
{
    public required string DeltaText { get; init; }
}
