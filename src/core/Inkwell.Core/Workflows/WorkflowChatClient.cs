using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows;

/// <summary>
/// 将 Workflow 包装为 IChatClient，使其能通过 ChatClientAgent 接入 AG-UI 协议
/// 设计目标：
///   1. 接口统一 —— 与 Agent 走相同的 AG-UI / Session 持久化链路
///   2. 能力感知 —— 根据 WorkflowCapabilities 决定是否传递多轮历史
///   3. HITL 友好 —— 自动响应 RequestInfoEvent，Workflow 内含人工审核节点时仍可跑通
/// </summary>
public sealed class WorkflowChatClient(
    Workflow workflow,
    WorkflowCapabilities? capabilities = null) : IChatClient
{
    private readonly WorkflowCapabilities _capabilities = capabilities ?? new();

    /// <inheritdoc />
    public ChatClientMetadata Metadata { get; } = new("WorkflowChatClient");

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        StringBuilder outputBuilder = new();

        await foreach (string fragment in this.RunWorkflowAsync(chatMessages, cancellationToken).ConfigureAwait(false))
        {
            outputBuilder.AppendLine(fragment);
        }

        string output = outputBuilder.Length > 0
            ? outputBuilder.ToString().TrimEnd()
            : "Workflow 执行完成，无输出内容。";

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, output));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (string fragment in this.RunWorkflowAsync(chatMessages, cancellationToken).ConfigureAwait(false))
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new TextContent(fragment)]
            };
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <summary>
    /// 执行 Workflow 并把关键事件翻译成可读文本片段
    /// 处理三类事件：
    ///   - WorkflowOutputEvent  最终产物，直接输出
    ///   - RequestInfoEvent     人工审核节点，自动批准以让流程继续（P2 将由前端按钮接管）
    ///   - 其他                 忽略
    /// </summary>
    private async IAsyncEnumerable<string> RunWorkflowAsync(
        IEnumerable<ChatMessage> chatMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string input = ExtractWorkflowInput(chatMessages, this._capabilities);

        await using StreamingRun run = await InProcessExecution
            .RunStreamingAsync(workflow, input, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            switch (evt)
            {
                case WorkflowOutputEvent outputEvent when outputEvent.Data is not null:
                    yield return outputEvent.Data.ToString() ?? string.Empty;
                    break;

                case RequestInfoEvent requestEvent when this._capabilities.SupportsHumanInLoop:
                    // 当前策略：自动批准，保证 OneShot 场景下 Workflow 能够走到终态
                    // 后续 P2 扩展：把 request.Data 送到前端渲染 approve/reject 按钮，
                    //               通过 /api/workflows/{id}/runs/{runId}/respond 驱动 StreamingRun.SendResponseAsync
                    ExternalResponse autoApproved = requestEvent.Request.CreateResponse(true);
                    await run.SendResponseAsync(autoApproved).ConfigureAwait(false);
                    yield return "[系统] 人工审核节点已自动批准";
                    break;
            }
        }
    }

    /// <summary>
    /// 根据能力标签决定 Workflow 入口的输入
    /// 多轮：拼接历史（占位，需入口 Executor 接受 List&lt;ChatMessage&gt; 才能真正发挥作用）
    /// 单轮：只取最后一条 User 消息
    /// </summary>
    private static string ExtractWorkflowInput(IEnumerable<ChatMessage> chatMessages, WorkflowCapabilities capabilities)
    {
        if (capabilities.SupportsMultiTurn)
        {
            StringBuilder builder = new();
            foreach (ChatMessage msg in chatMessages)
            {
                if (string.IsNullOrWhiteSpace(msg.Text))
                {
                    continue;
                }

                builder.Append(msg.Role.Value).Append(": ").AppendLine(msg.Text);
            }

            return builder.Length > 0 ? builder.ToString().TrimEnd() : "请输入主题";
        }

        string? lastUserMessage = null;
        foreach (ChatMessage msg in chatMessages)
        {
            if (msg.Role == ChatRole.User && !string.IsNullOrWhiteSpace(msg.Text))
            {
                lastUserMessage = msg.Text;
            }
        }

        return lastUserMessage ?? "请输入主题";
    }
}
