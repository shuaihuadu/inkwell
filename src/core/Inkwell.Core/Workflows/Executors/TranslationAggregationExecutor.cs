using Inkwell;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 翻译聚合 Executor
/// 将多个语言的翻译结果汇总为 MultiLanguageResult
/// Fan-In Barrier 可能逐一传入每种语言的结果，需要收集齐后再输出
/// 使用 WorkflowContext 状态（非实例字段），避免单例 Executor 跨运行污染
/// </summary>
[YieldsOutput(typeof(MultiLanguageResult))]
[SendsMessage(typeof(MultiLanguageResult))]
internal sealed class TranslationAggregationExecutor(int expectedCount = 3) : Executor<TranslationResult>("TranslationAggregation")
{
    private const string BufferKey = "translation-buffer";
    private const string OriginalKey = "translation-original";

    /// <summary>
    /// 设置原文，写入运行上下文状态；需在 Workflow 启动前由 Host 调用该 Executor 的 Execute 前 hook。
    /// 当前调用链仍保留实例方法兼容；建议改由上游 Executor 通过 SendMessage 传递。
    /// </summary>
    internal async ValueTask SetOriginalAsync(IWorkflowContext context, string original, CancellationToken cancellationToken = default)
    {
        await context.QueueStateUpdateAsync(OriginalKey, original, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask HandleAsync(TranslationResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        List<TranslationResult> results = await context.ReadStateAsync<List<TranslationResult>>(BufferKey, cancellationToken: cancellationToken) ?? [];
        results.Add(message);

        if (results.Count < expectedCount)
        {
            await context.QueueStateUpdateAsync(BufferKey, results, cancellationToken: cancellationToken);
            return;
        }

        await context.QueueStateUpdateAsync(BufferKey, new List<TranslationResult>(), cancellationToken: cancellationToken);

        string original = await context.ReadStateAsync<string>(OriginalKey, cancellationToken: cancellationToken) ?? string.Empty;

        MultiLanguageResult result = new()
        {
            Original = original,
            Translations = [.. results]
        };

        await context.SendMessageAsync(result, cancellationToken: cancellationToken);
        await context.YieldOutputAsync(result, cancellationToken: cancellationToken);
    }
}
