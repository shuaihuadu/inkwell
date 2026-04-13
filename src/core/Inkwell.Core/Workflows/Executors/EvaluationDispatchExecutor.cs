using Inkwell;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 批量评估分发 Executor
/// 接收文章列表，为每篇文章向下游 Fan-Out 发送评估请求
/// </summary>
[SendsMessage(typeof(ArticleEvaluation))]
internal sealed class EvaluationDispatchExecutor() : Executor<List<ArticleEvaluation>>("EvaluationDispatch")
{
    /// <inheritdoc />
    public override async ValueTask HandleAsync(List<ArticleEvaluation> message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // 将文章列表存入共享状态，供聚合时使用
        await context.QueueStateUpdateAsync("article_count", message.Count,
            scopeName: "evaluation", cancellationToken);

        // Fan-Out：依次发送每篇文章给下游评估器
        foreach (ArticleEvaluation article in message)
        {
            await context.SendMessageAsync(article, cancellationToken: cancellationToken);
        }
    }
}
