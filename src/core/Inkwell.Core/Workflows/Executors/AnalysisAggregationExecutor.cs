using Inkwell;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 选题分析汇总 Executor
/// 汇聚多个分析维度的结果，生成统一的分析报告，并存入共享状态
/// 注意：使用 WorkflowContext 状态而非实例字段，避免单例 Executor 跨运行状态污染
/// </summary>
[YieldsOutput(typeof(string))]
[SendsMessage(typeof(TopicAnalysis))]
internal sealed class AnalysisAggregationExecutor() : Executor<TopicAnalysis>("AnalysisAggregation")
{
    /// <summary>缓冲状态 Key，运行范围由 MAF 运行上下文隔离</summary>
    private const string BufferKey = "pending-analyses";

    /// <inheritdoc />
    public override async ValueTask HandleAsync(TopicAnalysis message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // 从运行上下文读取已缓冲的分析（首次读取为 null）
        List<TopicAnalysis> buffer = await context.ReadStateAsync<List<TopicAnalysis>>(BufferKey,
            scopeName: StateScopes.AnalysisScope, cancellationToken) ?? [];

        buffer.Add(message);

        // 等两个分析分支都完成后汇总
        if (buffer.Count < 2)
        {
            await context.QueueStateUpdateAsync(BufferKey, buffer,
                scopeName: StateScopes.AnalysisScope, cancellationToken);
            return;
        }

        TopicAnalysis merged = new()
        {
            Topic = buffer[0].Topic,
            MarketTrends = string.Join("\n\n", buffer.Select(a => a.MarketTrends).Where(s => !string.IsNullOrEmpty(s))),
            TargetAudience = string.Join("\n\n", buffer.Select(a => a.TargetAudience).Where(s => !string.IsNullOrEmpty(s))),
            ContentAngles = string.Join("\n\n", buffer.Select(a => a.ContentAngles).Where(s => !string.IsNullOrEmpty(s)))
        };

        // 清空缓冲，避免同一运行内再次触发时计数异常
        await context.QueueStateUpdateAsync(BufferKey, new List<TopicAnalysis>(),
            scopeName: StateScopes.AnalysisScope, cancellationToken);

        // 存入共享状态，供后续 Executor 使用
        await context.QueueStateUpdateAsync("analysis", merged,
            scopeName: StateScopes.AnalysisScope, cancellationToken);

        // 将汇总的分析报告发送给下游（写作 Executor）
        await context.SendMessageAsync(merged, cancellationToken: cancellationToken);

        // 通知外部分析完成（完整展示三段内容，超长再折叠）
        string preview = BuildPreview(merged);
        await context.YieldOutputAsync($"[选题分析完成]\n{preview}", cancellationToken);
    }

    /// <summary>
    /// 生成分析报告的预览文本；段落完整展示，仅当单段超过 500 字符时折叠
    /// </summary>
    private static string BuildPreview(TopicAnalysis merged)
    {
        string Trim(string? s, int max = 500)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return "（无）";
            }
            return s.Length <= max ? s : s[..max] + "……";
        }

        return $"""
            **主题**：{merged.Topic}

            **市场趋势**
            {Trim(merged.MarketTrends)}

            **目标受众**
            {Trim(merged.TargetAudience)}

            **内容角度**
            {Trim(merged.ContentAngles)}
            """;
    }
}
