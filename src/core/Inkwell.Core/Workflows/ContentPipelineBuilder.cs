using Inkwell;
using Inkwell.Workflows.Executors;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows;

/// <summary>
/// 内容生产流水线 Workflow 构建器
/// Phase 1: 选题分析(Fan-Out/Fan-In) → 内容创作(Writer-Critic Loop) → 人工审核(RequestPort)
/// </summary>
public static class ContentPipelineBuilder
{
    /// <summary>
    /// 构建内容生产流水线
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <param name="maxRevisions">最大修订次数</param>
    /// <returns>构建好的 Workflow 实例</returns>
    public static Workflow Build(IChatClient chatClient, int maxRevisions = 3)
    {
        // ========== 创建 Agent ==========

        // 市场趋势分析 Agent
        AIAgent marketAgent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new()
            {
                Instructions = """
                    你是一名市场研究分析师。请分析给定主题的市场趋势、目标受众和内容机会。
                    请以 JSON 格式回复：
                    {"topic": "...", "market_trends": "...", "target_audience": "...", "content_angles": "..."}
                    """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<TopicAnalysis>()
            }
        });

        // 竞品分析 Agent
        AIAgent competitorAgent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new()
            {
                Instructions = """
                    你是一名竞品内容分析师。请分析市场上已有的同类内容，并提出差异化的内容角度建议。
                    请以 JSON 格式回复：
                    {"topic": "...", "market_trends": "...", "target_audience": "...", "content_angles": "..."}
                    """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<TopicAnalysis>()
            }
        });

        // 写作 Agent
        AIAgent writerAgent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new()
            {
                Instructions = """
                    你是一名专业内容写作者。请撰写引人入胜、结构清晰的文章，
                    内容要信息丰富且对目标受众有吸引力。注重清晰度、
                    叙事性和可操作的见解。
                    """
            }
        });

        // 审核 Agent
        AIAgent criticAgent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new()
            {
                Instructions = """
                    你是一名严格的内容编辑。请从质量、准确性、吸引力和完整性四个维度审核文章。
                    提供建设性的反馈。请以 JSON 格式回复：
                    {"approved": true/false, "feedback": "详细反馈", "score": 1-10}
                    """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<ReviewDecision>()
            }
        });

        // ========== 创建 Executor ==========

        InputDispatchExecutor inputDispatch = new();
        MarketAnalysisExecutor marketAnalysis = new(marketAgent);
        CompetitorAnalysisExecutor competitorAnalysis = new(competitorAgent);
        AnalysisAggregationExecutor analysisAggregation = new();

        WriterExecutor writer = new(writerAgent);
        CriticExecutor critic = new(criticAgent, maxRevisions);

        // 人工审核端口：接收 Article，返回 bool（true=发布，false=退回）
        RequestPort reviewPort = RequestPort.Create<Article, bool>("HumanReview");
        ReviewGateExecutor reviewGate = new();

        // ========== 构建 Workflow ==========

        /*
         *   拓扑结构:
         *
         *   [输入: 主题(string)]
         *        │
         *        ▼
         *   InputDispatch ─── Fan-Out ──▶ MarketAnalysis ──┐
         *                               ▶ CompetitorAnalysis──┤ Fan-In
         *                                                      ▼
         *                                           AnalysisAggregation
         *                                                      │
         *                                                      ▼
         *                              ┌──── WriterExecutor ◀──────────┐
         *                              │           │                   │
         *                              │           ▼                   │ Critic退回
         *                              │    CriticExecutor ────────────┘
         *                              │           │
         *                              │           │ Critic通过
         *                              │           ▼
         *                              │    RequestPort(人工审核) ⏸
         *                              │           │
         *                              │           ▼
         *                              │    ReviewGateExecutor
         *                              │      │          │
         *                              │      │(发布)    │(退回)
         *                              │      ▼          └──▶ WriterExecutor
         *                              │   (最终输出)
         */

        return new WorkflowBuilder(inputDispatch)
            .WithName("ContentPipeline")
            .WithDescription("AI-powered content production pipeline with topic analysis, writing, review, and human approval")
            // Phase 1a: 选题分析 Fan-Out / Fan-In
            .AddFanOutEdge(inputDispatch, [marketAnalysis, competitorAnalysis])
            .AddFanInBarrierEdge([marketAnalysis, competitorAnalysis], analysisAggregation)
            // Phase 1b: 内容创作 Writer-Critic Loop
            .AddEdge(analysisAggregation, writer)
            .AddEdge(writer, critic)
            .AddSwitch(critic, sw => sw
                .AddCase<Article>(a => a?.Status == ArticleStatus.Approved, reviewPort)
                .WithDefault(writer))
            // Phase 1c: 人工审核
            .AddEdge(reviewPort, reviewGate)
            .AddEdge<TopicAnalysis>(reviewGate, writer, condition: null)
            .WithOutputFrom(analysisAggregation, reviewGate)
            .WithOpenTelemetry()
            .Build();
    }
}
