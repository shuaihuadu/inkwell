using System.Text.Json;
using Inkwell;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 内容审核 Executor
/// 审核文章质量，决定通过或退回修改
/// </summary>
[SendsMessage(typeof(TopicAnalysis))]
[SendsMessage(typeof(Article))]
internal sealed class CriticExecutor(AIAgent agent, int maxRevisions = 3) : Executor<Article>("Critic")
{
    /// <inheritdoc />
    public override async ValueTask HandleAsync(Article article, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string prompt = $$"""
            请审核以下文章并给出你的决定。

            标题：{{article.Title}}
            内容：
            {{article.Content}}

            当前版本：第 {{article.Revision}} 稿（最多 {{maxRevisions}} 稿）

            请从清晰度、吸引力、准确性和完整性四个维度评估。
            如果已达到最大修订次数（{{maxRevisions}}），请适当放宽标准。

            请以 JSON 格式回复：
            {"approved": true/false, "feedback": "你的详细反馈", "score": 1-10}
            """;

        AgentResponse response = await agent.RunAsync(prompt, cancellationToken: cancellationToken);

        ReviewDecision decision = JsonSerializer.Deserialize<ReviewDecision>(response.Text)
            ?? new ReviewDecision { Approved = true, Feedback = "整体质量较好。", Score = 7 };

        // 如果达到最大修订次数，强制通过
        if (article.Revision >= maxRevisions && !decision.Approved)
        {
            decision.Approved = true;
            decision.Feedback += " [Auto-approved: max revisions reached]";
        }

        // 存入共享状态
        await context.QueueStateUpdateAsync("review", decision,
            scopeName: StateScopes.ArticleScope, cancellationToken);

        if (decision.Approved)
        {
            // 通过：将文章发送给人工审核环节
            article.Status = ArticleStatus.Approved;
            await context.SendMessageAsync(article, cancellationToken: cancellationToken);
        }
        else
        {
            // 退回：将分析报告重新发给 Writer（触发修改循环）
            TopicAnalysis? analysis = await context.ReadStateAsync<TopicAnalysis>("analysis",
                scopeName: StateScopes.AnalysisScope, cancellationToken);

            await context.SendMessageAsync(analysis ?? new TopicAnalysis { Topic = article.Topic },
                cancellationToken: cancellationToken);
        }
    }
}
