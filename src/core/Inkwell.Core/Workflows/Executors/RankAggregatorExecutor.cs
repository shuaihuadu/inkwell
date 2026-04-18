using Inkwell;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 评估结果聚合排序 Executor
/// 收集所有文章评分，按综合得分降序排列输出报告
/// Fan-In Barrier 可能逐一传入每篇文章的结果，需要收集齐后再输出
/// 使用 WorkflowContext 状态（非实例字段），避免单例 Executor 跨运行污染
/// </summary>
[YieldsOutput(typeof(BatchEvaluationReport))]
[SendsMessage(typeof(BatchEvaluationReport))]
internal sealed class RankAggregatorExecutor(int expectedCount = 3) : Executor<ArticleScore>("RankAggregator")
{
    private const string BufferKey = "rank-buffer";

    /// <inheritdoc />
    public override async ValueTask HandleAsync(ArticleScore message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        List<ArticleScore> scores = await context.ReadStateAsync<List<ArticleScore>>(BufferKey, cancellationToken: cancellationToken) ?? [];
        scores.Add(message);

        // 仅在收齐所有评分后才输出
        if (scores.Count < expectedCount)
        {
            await context.QueueStateUpdateAsync(BufferKey, scores, cancellationToken: cancellationToken);
            return;
        }

        // 收齐后清空缓冲，供下次运行使用
        await context.QueueStateUpdateAsync(BufferKey, new List<ArticleScore>(), cancellationToken: cancellationToken);

        List<ArticleScore> ranked = [.. scores.OrderByDescending(s => s.TotalScore)];

        BatchEvaluationReport report = new()
        {
            Rankings = ranked,
            Summary = $"共评估 {ranked.Count} 篇文章。" +
                      $"最高分：{ranked.FirstOrDefault()?.Title}（{ranked.FirstOrDefault()?.TotalScore} 分），" +
                      $"最低分：{ranked.LastOrDefault()?.Title}（{ranked.LastOrDefault()?.TotalScore} 分）。"
        };

        await context.SendMessageAsync(report, cancellationToken: cancellationToken);
        await context.YieldOutputAsync(report, cancellationToken: cancellationToken);
    }
}
