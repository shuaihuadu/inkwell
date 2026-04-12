using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 翻译分发 Executor
/// 接收原始文本并传递给下游翻译 Executor
/// </summary>
[SendsMessage(typeof(string))]
internal sealed class TranslationDispatchExecutor() : Executor<string>("TranslationDispatch")
{
    /// <inheritdoc />
    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // 将原文传递给所有 Fan-Out 的翻译 Executor
        await context.SendMessageAsync(message, cancellationToken: cancellationToken);
    }
}
