using System.Text.Json;
using System.Text.RegularExpressions;
using Inkwell;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 内容审核 Executor
/// 审核文章质量，决定通过或退回修改
/// </summary>
[SendsMessage(typeof(TopicAnalysis))]
[SendsMessage(typeof(Article))]
internal sealed partial class CriticExecutor(AIAgent agent, int maxRevisions = 3, ILogger<CriticExecutor>? logger = null) : Executor<Article>("Critic")
{
    /// <summary>
    /// 反序列化时使用的 JSON 选项（容忍属性名大小写、注释、尾随逗号）
    /// </summary>
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    [GeneratedRegex(@"```(?:json)?\s*(\{[\s\S]*?\})\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex JsonFenceRegex();

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

        string text = await agent.RunAndStreamAsync(prompt, this.Id, context, cancellationToken);

        ReviewDecision decision = TryParseReviewDecision(text, logger);

        // 如果达到最大修订次数，强制通过
        if (article.Revision >= maxRevisions && !decision.Approved)
        {
            decision.Approved = true;
            decision.Feedback += " [Auto-approved: max revisions reached]";
        }

        logger?.LogInformation("[Critic] Title={Title} Revision={Revision} Approved={Approved} Score={Score}",
            article.Title, article.Revision, decision.Approved, decision.Score);

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

    /// <summary>
    /// 健壮地解析 LLM 返回的 ReviewDecision JSON
    /// 兼容三种格式：纯 JSON / Markdown 代码块包裹 / 文本中嵌入对象
    /// 任何失败都回退到一个保守的"通过"决策，避免 Workflow 链路中断
    /// </summary>
    private static ReviewDecision TryParseReviewDecision(string raw, ILogger? log)
    {
        ReviewDecision Fallback(string reason)
        {
            log?.LogWarning("[Critic] Fallback ReviewDecision applied: {Reason}. Raw={Raw}",
                reason, raw.Length > 200 ? raw[..200] : raw);
            return new ReviewDecision { Approved = true, Feedback = "整体质量较好（解析失败已自动通过）。", Score = 7 };
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return Fallback("empty response");
        }

        // 1. 直接尝试
        ReviewDecision? direct = TryDeserialize(raw);
        if (direct is not null)
        {
            return direct;
        }

        // 2. 提取 ```json {...} ``` 包裹
        Match fence = JsonFenceRegex().Match(raw);
        if (fence.Success)
        {
            ReviewDecision? fromFence = TryDeserialize(fence.Groups[1].Value);
            if (fromFence is not null)
            {
                return fromFence;
            }
        }

        // 3. 取第一个 { 到最后一个 } 之间的子串
        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            ReviewDecision? sliced = TryDeserialize(raw[start..(end + 1)]);
            if (sliced is not null)
            {
                return sliced;
            }
        }

        return Fallback("no parsable JSON");
    }

    private static ReviewDecision? TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ReviewDecision>(json, s_jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
