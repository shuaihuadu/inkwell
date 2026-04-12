using Inkwell;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 审核结果处理 Executor
/// 接收人工审核的 bool 结果，决定发布或退回
/// </summary>
[YieldsOutput(typeof(string))]
[SendsMessage(typeof(TopicAnalysis))]
internal sealed class ReviewGateExecutor() : Executor<bool>("ReviewGate")
{
    /// <inheritdoc />
    public override async ValueTask HandleAsync(bool approved, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (approved)
        {
            // 人工审核通过：从共享状态读取文章，产出最终输出
            Article? article = await context.ReadStateAsync<Article>("current",
                scopeName: StateScopes.ArticleScope, cancellationToken);

            await context.YieldOutputAsync(
                $"[已发布] {article?.Title ?? "未知"}\n\n{article?.Content ?? ""}",
                cancellationToken);
        }
        else
        {
            // 人工审核退回：将分析报告重新发给 Writer 触发修改
            TopicAnalysis? analysis = await context.ReadStateAsync<TopicAnalysis>("analysis",
                scopeName: StateScopes.AnalysisScope, cancellationToken);

            Article? article = await context.ReadStateAsync<Article>("current",
                scopeName: StateScopes.ArticleScope, cancellationToken);

            // 更新审核反馈为"人工退回"
            ReviewDecision humanReview = new()
            {
                Approved = false,
                Feedback = "Human reviewer requested revisions.",
                Score = 0
            };
            await context.QueueStateUpdateAsync("review", humanReview,
                scopeName: StateScopes.ArticleScope, cancellationToken);

            await context.SendMessageAsync(
                analysis ?? new TopicAnalysis { Topic = article?.Topic ?? "Unknown" },
                cancellationToken: cancellationToken);
        }
    }
}
