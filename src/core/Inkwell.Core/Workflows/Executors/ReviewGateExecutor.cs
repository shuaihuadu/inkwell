using System.Text.Json;
using Inkwell;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 审核结果处理 Executor
/// 接收人工审核的 bool 结果，决定发布或退回
/// </summary>
/// <remarks>
/// 发布路径有两条：
/// <list type="bullet">
/// <item>优先走 <c>publisherAgent</c>：让 LLM 调用 <c>publish_article</c> AIFunction 完成入库，
/// 演示 AIFunction 在工作流中的用法；</item>
/// <item>若 Agent 未配置或调用异常，则兜底直接通过 <see cref="ArticleWriteGateway"/> 写入，
/// 确保已审核通过的文章不会因 LLM 行为抖动而丢失。</item>
/// </list>
/// </remarks>
[YieldsOutput(typeof(string))]
[SendsMessage(typeof(TopicAnalysis))]
internal sealed class ReviewGateExecutor(
    ArticleWriteGateway? articleGateway = null,
    AIAgent? publisherAgent = null,
    ILogger<ReviewGateExecutor>? logger = null) : Executor<bool>("ReviewGate")
{
    private static readonly JsonSerializerOptions PublishPromptJsonOptions = new()
    {
        WriteIndented = false
    };

    /// <inheritdoc />
    public override async ValueTask HandleAsync(bool approved, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (approved)
        {
            // 人工审核通过：从共享状态读取文章，持久化并产出最终输出
            Article? article = await context.ReadStateAsync<Article>("current",
                scopeName: StateScopes.ArticleScope, cancellationToken);

            // 审核通过是确定性最终动作：AG-UI SSE 关闭会连带取消上游 token，
            // 这里改用独立的 60s 超时 token，避免落库被误取消
            using CancellationTokenSource publishCts = new(TimeSpan.FromSeconds(60));
            CancellationToken publishToken = publishCts.Token;

            if (article is not null)
            {
                bool published = await this.TryPublishViaAgentAsync(article, context, publishToken).ConfigureAwait(false);

                // Agent 路径失败或未配置时，走直连 Gateway 的兜底路径
                if (!published && articleGateway is not null)
                {
                    ArticleRecord record = new()
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Topic = article.Topic,
                        Title = article.Title,
                        Content = article.Content,
                        Status = nameof(ArticleStatus.Published),
                        Revision = article.Revision,
                        CreatedAt = article.CreatedAt,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    try
                    {
                        await articleGateway.AddAsync(record, logger, publishToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "[ReviewGateExecutor] Fallback persist failed. Id={Id} Title={Title}", record.Id, record.Title);
                    }
                }
            }

            await context.YieldOutputAsync(
                $"[已发布] {article?.Title ?? "未知"}\n\n{article?.Content ?? ""}",
                cancellationToken);
        }
        else
        {
            // 人工审核退回：将分析报告重新发给 Writer 触发修改
            TopicAnalysis? analysis = await context.ReadStateAsync<TopicAnalysis>("analysis",
                scopeName: StateScopes.AnalysisScope, cancellationToken);

            Article? article = await context.ReadStateAsync<Article>("current",
                scopeName: StateScopes.ArticleScope, cancellationToken);

            // 更新审核反馈为"人工退回"
            ReviewDecision humanReview = new()
            {
                Approved = false,
                Feedback = "Human reviewer requested revisions.",
                Score = 0
            };
            await context.QueueStateUpdateAsync("review", humanReview,
                scopeName: StateScopes.ArticleScope, cancellationToken);

            await context.SendMessageAsync(
                analysis ?? new TopicAnalysis { Topic = article?.Topic ?? "Unknown" },
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 通过 PublisherAgent 调用 <c>publish_article</c> AIFunction 发布文章
    /// </summary>
    /// <returns>发布是否成功</returns>
    private async Task<bool> TryPublishViaAgentAsync(Article article, IWorkflowContext context, CancellationToken cancellationToken)
    {
        if (publisherAgent is null)
        {
            logger?.LogWarning("[ReviewGateExecutor] PublisherAgent not configured, will fallback to direct gateway. Title={Title}", article.Title);
            return false;
        }

        logger?.LogInformation("[ReviewGateExecutor] Invoking PublisherAgent. Title={Title}", article.Title);

        string payload = JsonSerializer.Serialize(new
        {
            topic = article.Topic,
            title = article.Title,
            content = article.Content,
            revision = article.Revision
        }, PublishPromptJsonOptions);

        string prompt = $"请调用 publish_article 工具发布下面这篇文章，不要输出其他内容：\n{payload}";

        try
        {
            // 走流式路径，把工具调用事件透传到 Workflow 事件流
            string text = await publisherAgent.RunAndStreamAsync(prompt, this.Id, context, cancellationToken).ConfigureAwait(false);
            logger?.LogInformation("[ReviewGateExecutor] PublisherAgent finished. Title={Title} Response={Response}",
                article.Title, text);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "[ReviewGateExecutor] PublisherAgent failed, will fallback to gateway. Title={Title}", article.Title);
            return false;
        }
    }
}
