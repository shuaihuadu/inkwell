using System.Text.Json;
using Inkwell;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 文章评估 Executor
/// 调用 AI Agent 对单篇文章进行多维度评分
/// </summary>
[SendsMessage(typeof(ArticleScore))]
internal sealed class ArticleEvaluatorExecutor(AIAgent agent, string evaluatorId) : Executor<ArticleEvaluation>(evaluatorId)
{
    /// <inheritdoc />
    public override async ValueTask HandleAsync(ArticleEvaluation message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string prompt = $"""
            请对以下文章进行多维度评估，以 JSON 格式返回评分结果。

            文章标题：{message.Title}
            文章内容：
            {message.Content[..Math.Min(message.Content.Length, 2000)]}

            请评估以下维度（每项 1-10 分）：
            1. readability_score（可读性）：语言流畅、结构清晰、易于理解
            2. seo_score（SEO）：标题优化、关键词使用、元描述友好
            3. originality_score（原创性）：独特视角、非泛泛而谈
            4. total_score（综合得分）：三项平均分
            5. feedback：简短的改进建议（50字以内）
            """;

        AgentResponse response = await agent.RunAsync(prompt, cancellationToken: cancellationToken);

        ArticleScore score;
        try
        {
            score = JsonSerializer.Deserialize<ArticleScore>(response.Text) ?? new ArticleScore();
        }
        catch
        {
            score = new ArticleScore { Feedback = response.Text };
        }

        score.Title = message.Title;
        if (score.TotalScore == 0)
        {
            score.TotalScore = Math.Round((score.ReadabilityScore + score.SeoScore + score.OriginalityScore) / 3.0, 1);
        }

        await context.SendMessageAsync(score, cancellationToken: cancellationToken);
    }
}
