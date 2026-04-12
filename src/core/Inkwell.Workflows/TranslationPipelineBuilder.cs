using Inkwell.Workflows.Executors;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows;

/// <summary>
/// 多语言翻译流水线构建器
/// 一篇文章同时翻译为多种语言（Fan-Out / Fan-In 模式）
/// </summary>
public static class TranslationPipelineBuilder
{
    /// <summary>
    /// 构建多语言翻译流水线
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <param name="targetLanguages">目标语言列表</param>
    /// <returns>构建好的 Workflow 实例</returns>
    public static Workflow Build(IChatClient chatClient, string[]? targetLanguages = null)
    {
        targetLanguages ??= ["English", "Japanese", "French"];

        // ========== 创建 Executor ==========

        TranslationDispatchExecutor dispatch = new();

        // 为每种语言创建一个翻译 Executor
        List<TranslatorExecutor> translators = [];
        foreach (string language in targetLanguages)
        {
            AIAgent translatorAgent = chatClient.AsAIAgent(
                instructions: $"""
                    你是一名专业翻译。将用户提供的内容准确翻译成{language}。
                    保持原文的语气、风格和格式。确保翻译自然流畅，符合目标语言的表达习惯。
                    只返回翻译后的内容，不要添加任何说明。
                    """);

            translators.Add(new TranslatorExecutor(translatorAgent, language));
        }

        TranslationAggregationExecutor aggregation = new();

        // ========== 构建 Workflow ==========

        /*
         *   拓扑结构:
         *
         *   [输入: 文章(string)]
         *        │
         *        ▼
         *   TranslationDispatch
         *        │
         *        ├── Fan-Out → Translator_English ──┐
         *        ├── Fan-Out → Translator_Japanese ──┤ Fan-In Barrier
         *        └── Fan-Out → Translator_French ────┘
         *                                             │
         *                                             ▼
         *                                   TranslationAggregation
         *                                             │
         *                                             ▼
         *                                      [输出: MultiLanguageResult]
         */

        ExecutorBinding[] translatorBindings = translators.Select(t => (ExecutorBinding)t).ToArray();

        WorkflowBuilder builder = new WorkflowBuilder(dispatch)
            .WithName("TranslationPipeline")
            .WithDescription("Multi-language translation pipeline using Fan-Out/Fan-In pattern")
            .AddFanOutEdge(dispatch, translatorBindings)
            .AddFanInBarrierEdge(translatorBindings, aggregation)
            .WithOutputFrom(aggregation)
            .WithOpenTelemetry();

        return builder.Build();
    }
}
