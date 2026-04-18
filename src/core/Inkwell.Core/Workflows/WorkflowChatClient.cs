using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows;

/// <summary>
/// 将 Workflow 包装为 IChatClient，使其能通过 ChatClientAgent 接入 AG-UI 协议
/// 通过 InProcessExecution.RunStreamingAsync 执行 Workflow，收集 WorkflowOutputEvent 作为回复
/// </summary>
public sealed class WorkflowChatClient(Workflow workflow) : IChatClient
{
    /// <inheritdoc />
    public ChatClientMetadata Metadata => new("WorkflowChatClient");

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        string input = ExtractUserInput(chatMessages);

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input, cancellationToken: cancellationToken);

        StringBuilder outputBuilder = new();

        await foreach (WorkflowEvent evt in run.WatchStreamAsync(cancellationToken))
        {
            if (evt is WorkflowOutputEvent outputEvent)
            {
                outputBuilder.AppendLine(outputEvent.Data?.ToString());
            }
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
        string input = ExtractUserInput(chatMessages);

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input, cancellationToken: cancellationToken);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync(cancellationToken))
        {
            if (evt is WorkflowOutputEvent outputEvent && outputEvent.Data is not null)
            {
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Contents = [new TextContent(outputEvent.Data.ToString()!)]
                };
            }
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <summary>
    /// 从消息列表中提取最后一条用户消息作为 Workflow 输入
    /// </summary>
    private static string ExtractUserInput(IEnumerable<ChatMessage> chatMessages)
    {
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
