namespace Inkwell;

/// <summary>对应 AG-UI <c>tool_call</c>。</summary>
public sealed record class ToolCallResult : AgentRunEvent
{
    public required string ToolCallId { get; init; }

    public required string ResultJson { get; init; }

    public required bool IsError { get; init; }
}
