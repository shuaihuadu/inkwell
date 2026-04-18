using Inkwell;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 翻译聚合 Executor
/// 将多个语言的翻译结果汇总为 MultiLanguageResult
/// Fan-In Barrier 可能逐一传入每种语言的结果，需要收集齐后再输出
/// </summary>
[YieldsOutput(typeof(MultiLanguageResult))]
[SendsMessage(typeof(MultiLanguageResult))]
internal sealed class TranslationAggregationExecutor(int expectedCount = 3) : Executor<TranslationResult>("TranslationAggregation")
{
    private readonly List<TranslationResult> _results = [];
    private string _original = string.Empty;

    /// <summary>
    /// 设置原文（在 Workflow 启动时调用）
    /// </summary>
    internal void SetOriginal(string original)
    {
        this._original = original;
    }

    /// <inheritdoc />
    public override async ValueTask HandleAsync(TranslationResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        this._results.Add(message);

        // [M6 修复] 仅在收齐所有翻译结果后才输出
        if (this._results.Count < expectedCount)
        {
            return;
        }

        MultiLanguageResult result = new()
        {
            Original = this._original,
            Translations = [.. this._results]
        };

        await context.SendMessageAsync(result, cancellationToken: cancellationToken);
        await context.YieldOutputAsync(result, cancellationToken: cancellationToken);
    }
}
