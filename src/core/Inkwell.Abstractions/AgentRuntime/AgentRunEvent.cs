namespace Inkwell;

/// <summary>
/// <see cref="IAgentRuntime.RunStreamingAsync(AgentRunRequest, CancellationToken)"/> 产出的流式事件封闭子类型族；
/// 1:1 对应 AG-UI 的 message / tool_call / state_delta / lifecycle 四大类事件。
/// </summary>
public abstract record class AgentRunEvent
{
    public required string RunId { get; init; }
}

