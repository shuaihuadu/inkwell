namespace Inkwell;

/// <summary>
/// <see cref="IAgentRuntime.RunAsync(AgentRunRequest, CancellationToken)"/>（非流式）返回的完整结果；
/// 携带最终响应消息 + 完整工具调用回溯。
/// </summary>
public sealed record class AgentTurnResult
{
    public required string RunId { get; init; }

    public required AgentChatMessage Message { get; init; }

    public required IReadOnlyList<AgentToolCallRecord> ToolCalls { get; init; }

    public required string ModelIdUsed { get; init; }

    public required AgentModelParameters ModelParametersUsed { get; init; }
}
