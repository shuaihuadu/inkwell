using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows;

/// <summary>
/// Agent 流式调用辅助扩展
/// </summary>
/// <remarks>
/// <para>
/// 所有在 Executor 内调用 Agent 的地方统一走这里，把 <see cref="AgentResponseUpdate"/>
/// 通过 <see cref="IWorkflowContext.AddEventAsync"/> 向上抛出 <see cref="AgentResponseUpdateEvent"/>，
/// 让 <c>WorkflowChatClient</c> 能感知到 token / tool_call / tool_result 三类增量，
/// 进而以 AG-UI 标记形式透传给前端。
/// </para>
/// <para>
/// 该扩展并不关心业务语义：Executor 仍然自行解析返回文本、构造领域对象并写状态，
/// 这里只是把"可观测流"这件事集中在一个地方，避免每个 Executor 重复写 foreach。
/// </para>
/// </remarks>
internal static class AgentStreamingHelpers
{
    /// <summary>
    /// 流式运行 Agent，把每一段 <see cref="AgentResponseUpdate"/> 以
    /// <see cref="AgentResponseUpdateEvent"/> 形式抛到 Workflow 事件流，最终返回完整聚合文本
    /// </summary>
    /// <param name="agent">目标 Agent</param>
    /// <param name="prompt">用户消息</param>
    /// <param name="executorId">调用方 Executor 的 Id，用于前端标注归属</param>
    /// <param name="context">Workflow 上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Agent 最终产出的纯文本（多段 Text 内容拼接）</returns>
    public static async Task<string> RunAndStreamAsync(
        this AIAgent agent,
        string prompt,
        string executorId,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        StringBuilder textBuilder = new();

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(prompt, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            // 先向 Workflow 抛事件——前端可立即看到 token / tool_call 增量
            await context.AddEventAsync(new AgentResponseUpdateEvent(executorId, update), cancellationToken)
                .ConfigureAwait(false);

            // 再累积文本给 Executor 自己构造领域对象
            string? text = update.Text;
            if (!string.IsNullOrEmpty(text))
            {
                textBuilder.Append(text);
            }
        }

        return textBuilder.ToString();
    }
}
