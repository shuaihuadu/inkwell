using System.Text.Json;
using Inkwell;
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
                请根据审核反馈修改以下文章。

                原文：
                {existingArticle.Content}

                审核反馈：
                {review?.Feedback ?? "请提升文章质量。"}

                选题分析参考：
                市场趋势：{message.MarketTrends}
                目标受众：{message.TargetAudience}
                内容角度：{message.ContentAngles}

                请写出改进版本，只返回文章内容。
                """;
        }
        else
        {
            // 初稿：基于分析报告撰写
            prompt = $"""
                请撰写一篇关于以下主题的高质量文章：{message.Topic}

                请参考以下分析来指导写作：
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

        // 存入共享状态
        await context.QueueStateUpdateAsync("current", article,
            scopeName: StateScopes.ArticleScope, cancellationToken);

        // 发送给下游（审核 Executor）
        await context.SendMessageAsync(article, cancellationToken: cancellationToken);
    }
}
