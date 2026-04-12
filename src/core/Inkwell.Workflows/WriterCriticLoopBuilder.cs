using Inkwell;
using Inkwell.Workflows.Executors;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows;

/// <summary>
/// Writer-Critic 循环构建器
/// 独立的写作-审核迭代循环，直到审核通过或达到最大轮次
/// </summary>
public static class WriterCriticLoopBuilder
{
    /// <summary>
    /// 构建 Writer-Critic 循环 Workflow
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <param name="maxRevisions">最大修订次数</param>
    /// <returns>构建好的 Workflow 实例</returns>
    public static Workflow Build(IChatClient chatClient, int maxRevisions = 3)
    {
        // ========== 创建 Agent ==========

        AIAgent writerAgent = chatClient.AsAIAgent(
            instructions: """
                你是一名专业内容写作者。请撰写引人入胜、结构清晰的文章，
                内容要信息丰富且对目标受众有吸引力。注重清晰度、叙事性和可操作的见解。
                如果收到修改反馈，请根据反馈完善文章。请用中文回复。
                """);

        AIAgent criticAgent = chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
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

        LoopWriterExecutor writer = new(writerAgent);
        CriticExecutor critic = new(criticAgent, maxRevisions);

        // ========== 构建 Workflow ==========

        /*
         *   拓扑结构:
         *
         *   [输入: 主题(TopicAnalysis)]
         *        │
         *        ▼
         *     Writer ◀───────┐
         *        │           │ Critic 退回
         *        ▼           │
         *     Critic ────────┘
         *        │
         *        │ 通过
         *        ▼
         *   [输出: Article]
         */

        return new WorkflowBuilder(writer)
            .WithName("WriterCriticLoop")
            .WithDescription("Writer-Critic iterative loop until approval or max revisions")
            .AddEdge(writer, critic)
            .AddSwitch(critic, sw => sw
                .AddCase<Article>(a => a?.Status == ArticleStatus.Approved, null!)
                .WithDefault(writer))
            .WithOutputFrom(critic)
            .Build();
    }
}

/// <summary>
/// 循环版写作 Executor（独立于内容流水线的简化版）
/// </summary>
[SendsMessage(typeof(Article))]
internal sealed class LoopWriterExecutor(AIAgent agent) : Executor<TopicAnalysis>("LoopWriter")
{
    private int _revision;

    /// <inheritdoc />
    public override async ValueTask HandleAsync(TopicAnalysis message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        this._revision++;

        Article? existingArticle = await context.ReadStateAsync<Article>("current",
            scopeName: StateScopes.ArticleScope, cancellationToken);

        string prompt;
        if (existingArticle is not null && this._revision > 1)
        {
            ReviewDecision? review = await context.ReadStateAsync<ReviewDecision>("review",
                scopeName: StateScopes.ArticleScope, cancellationToken);

            prompt = $"""
                请根据审核反馈修改以下文章。

                原文：
                {existingArticle.Content}

                审核反馈：
                {review?.Feedback ?? "请提升文章质量。"}

                主题信息：{message.Topic}

                请写出改进版本，只返回文章内容。
                """;
        }
        else
        {
            prompt = $"""
                请撰写一篇关于以下主题的高质量文章：{message.Topic}

                市场趋势：{message.MarketTrends}
                目标受众：{message.TargetAudience}
                内容角度：{message.ContentAngles}

                请撰写 300-500 字的文章，只返回文章内容。
                """;
        }

        AgentResponse response = await agent.RunAsync(prompt, cancellationToken: cancellationToken);

        Article article = new()
        {
            Topic = message.Topic,
            Title = message.Topic,
            Content = response.Text,
            Status = ArticleStatus.InReview,
            Revision = this._revision
        };

        await context.QueueStateUpdateAsync("current", article,
            scopeName: StateScopes.ArticleScope, cancellationToken);

        await context.SendMessageAsync(article, cancellationToken: cancellationToken);
    }
}
