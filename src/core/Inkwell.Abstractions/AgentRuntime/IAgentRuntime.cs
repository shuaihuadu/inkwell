namespace Inkwell;

/// <summary>
/// 顶层 Agent 执行引擎 facade。接口本身不出现任何 <c>Microsoft.Agents.AI.*</c> 类型
/// （ADR-017 §依赖规则第 3 条）；实现由 <c>Inkwell.Core</c> 的 <c>AgentRuntime/</c> 目录提供。
/// </summary>
public interface IAgentRuntime
{
    Task<AgentTurnResult> RunAsync(AgentRunRequest request, CancellationToken ct = default);

    IAsyncEnumerable<AgentRunEvent> RunStreamingAsync(AgentRunRequest request, CancellationToken ct = default);

    /// <summary>用户主动中断在途 Run；<c>true</c> = 找到并已请求中断，<c>false</c> = runId 未知或该 Run 已结束（幂等）。</summary>
    Task<bool> CancelRunAsync(string runId, CancellationToken ct = default);
}
