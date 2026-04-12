using Inkwell;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 评估结果聚合排序 Executor
/// 收集所有文章评分，按综合得分降序排列输出报告
/// </summary>
[SendsMessage(typeof(BatchEvaluationReport))]
internal sealed class RankAggregatorExecutor() : Executor<ArticleScore>("RankAggregator")
{
    private readonly List<ArticleScore> _scores = [];

    /// <inheritdoc />
    public override async ValueTask HandleAsync(ArticleScore message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        this._scores.Add(message);

        // Fan-In Barrier 保证所有评分完成才进入此 Executor
        List<ArticleScore> ranked = [.. this._scores.OrderByDescending(s => s.TotalScore)];

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
