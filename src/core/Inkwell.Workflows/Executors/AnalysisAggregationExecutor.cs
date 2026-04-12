using Inkwell;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 选题分析汇总 Executor
/// 汇聚多个分析维度的结果，生成统一的分析报告，并存入共享状态
/// </summary>
[YieldsOutput(typeof(string))]
[SendsMessage(typeof(TopicAnalysis))]
internal sealed class AnalysisAggregationExecutor() : Executor<TopicAnalysis>("AnalysisAggregation")
{
    private readonly List<TopicAnalysis> _analyses = [];

    /// <inheritdoc />
    public override async ValueTask HandleAsync(TopicAnalysis message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        this._analyses.Add(message);

        // 等两个分析分支都完成后汇总
        if (this._analyses.Count == 2)
        {
            TopicAnalysis merged = new()
            {
                Topic = this._analyses[0].Topic,
                MarketTrends = string.Join("\n\n", this._analyses.Select(a => a.MarketTrends).Where(s => !string.IsNullOrEmpty(s))),
                TargetAudience = string.Join("\n\n", this._analyses.Select(a => a.TargetAudience).Where(s => !string.IsNullOrEmpty(s))),
                ContentAngles = string.Join("\n\n", this._analyses.Select(a => a.ContentAngles).Where(s => !string.IsNullOrEmpty(s)))
            };

            // 存入共享状态，供后续 Executor 使用
            await context.QueueStateUpdateAsync("analysis", merged,
                scopeName: StateScopes.AnalysisScope, cancellationToken);

            // 将汇总的分析报告发送给下游（写作 Executor）
            await context.SendMessageAsync(merged, cancellationToken: cancellationToken);

            // 通知外部分析完成
            string preview = string.IsNullOrEmpty(merged.MarketTrends)
                ? merged.Topic
                : merged.MarketTrends[..Math.Min(100, merged.MarketTrends.Length)] + "...";

            await context.YieldOutputAsync(
                $"[选题分析完成] 主题: {merged.Topic}\n趋势: {preview}",
                cancellationToken);
        }
    }
}
