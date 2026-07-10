namespace Inkwell;

/// <summary>对应 AG-UI <c>tool_call</c>。</summary>
public sealed record class ToolCallRequested : AgentRunEvent
{
    public required string ToolCallId { get; init; }

    public required string ToolName { get; init; }

    public required string ArgumentsJson { get; init; }
}
