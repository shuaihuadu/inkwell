using Inkwell;
using Inkwell.Workflows.Executors;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows;

/// <summary>
/// 批量内容评估 MapReduce 构建器
/// 对 N 篇文章同时进行多维度评估，汇总排序
/// </summary>
public static class BatchEvaluationBuilder
{
    /// <summary>
    /// 构建批量评估 Workflow
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <param name="evaluatorCount">并行评估器数量</param>
    /// <returns>构建好的 Workflow 实例</returns>
    public static Workflow Build(IChatClient chatClient, int evaluatorCount = 3)
    {
        // ========== 创建 Agent ==========

        AIAgent evaluatorAgent = chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    你是一名内容质量评估专家。请对文章进行可读性、SEO、原创性三个维度的评分。
                    每个维度 1-10 分。请以 JSON 格式返回结果。
                    """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<ArticleScore>()
            }
        });

        // ========== 创建 Executor ==========

        EvaluationDispatchExecutor dispatch = new();

        // 创建 N 个并行评估器（动态 Fan-Out）
        List<ArticleEvaluatorExecutor> evaluators = [];
        for (int i = 0; i < evaluatorCount; i++)
        {
            evaluators.Add(new ArticleEvaluatorExecutor(evaluatorAgent, $"Evaluator_{i}"));
        }

        RankAggregatorExecutor aggregator = new();

        // ========== 构建 Workflow ==========

        /*
         *   MapReduce 拓扑：
         *
         *   [输入: List<ArticleEvaluation>]
         *        │
         *        ▼
         *   EvaluationDispatch
         *        │
         *        ├── Fan-Out → Evaluator_0 ──┐
         *        ├── Fan-Out → Evaluator_1 ──┤ Fan-In Barrier
         *        └── Fan-Out → Evaluator_2 ──┘
         *                                     │
         *                                     ▼
         *                              RankAggregator → [输出: BatchEvaluationReport]
         */

        ExecutorBinding[] evaluatorBindings = evaluators.Select(e => (ExecutorBinding)e).ToArray();

        return new WorkflowBuilder(dispatch)
            .WithName("BatchEvaluation")
            .WithDescription("MapReduce batch article evaluation with dynamic fan-out and ranking aggregation")
            .AddFanOutEdge(dispatch, evaluatorBindings)
            .AddFanInBarrierEdge(evaluatorBindings, aggregator)
            .WithOutputFrom(aggregator)
            .Build();
    }
}
