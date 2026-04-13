using Inkwell;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 输入分发 Executor
/// 接收用户输入的主题字符串，原样传递给下游（作为 Fan-Out 的起始节点）
/// </summary>
internal sealed class InputDispatchExecutor() : Executor<string, string>("InputDispatch")
{
    /// <inheritdoc />
    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(message);
    }
}
