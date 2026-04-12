using System.Text.Json;
using Inkwell.Core;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 内容写作 Executor
/// 基于选题分析报告撰写文章，或根据审核反馈修改文章
/// </summary>
[SendsMessage(typeof(Article))]
internal sealed class WriterExecutor(AIAgent agent) : Executor<TopicAnalysis>("Writer")
{
    private int _revision;

    /// <inheritdoc />
    public override async ValueTask HandleAsync(TopicAnalysis message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        this._revision++;

        // 从共享状态读取已有的文章（如果是修改稿）
        Article? existingArticle = await context.ReadStateAsync<Article>("current",
            scopeName: StateScopes.ArticleScope, cancellationToken);

        string prompt;
        if (existingArticle is not null && this._revision > 1)
        {
            // 修改稿：基于审核反馈重写
            ReviewDecision? review = await context.ReadStateAsync<ReviewDecision>("review",
                scopeName: StateScopes.ArticleScope, cancellationToken);

            prompt = $"""
                Please revise the following article based on the feedback.

                Original article:
                {existingArticle.Content}

                Feedback:
                {review?.Feedback ?? "Please improve the quality."}

                Topic analysis for reference:
                Market trends: {message.MarketTrends}
                Target audience: {message.TargetAudience}
                Content angles: {message.ContentAngles}

                Please write an improved version. Respond with the full revised article only.
                """;
        }
        else
        {
            // 初稿：基于分析报告撰写
            prompt = $"""
                Write a high-quality article about: {message.Topic}

                Use the following analysis to guide your writing:
                Market trends: {message.MarketTrends}
                Target audience: {message.TargetAudience}
                Content angles: {message.ContentAngles}

                Write a compelling article of 300-500 words. Respond with the article only.
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

        // 存入共享状态
        await context.QueueStateUpdateAsync("current", article,
            scopeName: StateScopes.ArticleScope, cancellationToken);

        // 发送给下游（审核 Executor）
        await context.SendMessageAsync(article, cancellationToken: cancellationToken);
    }
}
