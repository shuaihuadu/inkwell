namespace Inkwell;

/// <summary>对应 AG-UI <c>state_delta</c>。</summary>
public sealed record class StateDelta : AgentRunEvent
{
    public required string StateJsonPatch { get; init; }
}
