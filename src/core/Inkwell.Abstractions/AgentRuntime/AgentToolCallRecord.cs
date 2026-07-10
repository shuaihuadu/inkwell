namespace Inkwell;

/// <summary>非流式路径下单次工具调用的回溯记录。</summary>
public sealed record class AgentToolCallRecord
{
    public required string ToolCallId { get; init; }

    public required string ToolName { get; init; }

    public required string ArgumentsJson { get; init; }

    public required string ResultJson { get; init; }

    public required bool IsError { get; init; }
}
