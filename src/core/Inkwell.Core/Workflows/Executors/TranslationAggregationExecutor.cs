using Inkwell;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 翻译聚合 Executor
/// 将多个语言的翻译结果汇总为 MultiLanguageResult
/// </summary>
[SendsMessage(typeof(MultiLanguageResult))]
internal sealed class TranslationAggregationExecutor() : Executor<TranslationResult>("TranslationAggregation")
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

        // Fan-In Barrier 保证所有翻译完成才进入此 Executor
        // 聚合后输出最终结果
        MultiLanguageResult result = new()
        {
            Original = this._original,
            Translations = [.. this._results]
        };

        await context.SendMessageAsync(result, cancellationToken: cancellationToken);

        // 输出到 Workflow
        await context.YieldOutputAsync(result, cancellationToken: cancellationToken);
    }
}
