using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Inkwell.Agents.Middleware;

/// <summary>
/// 内容安全护栏中间件
/// 检测 Agent 生成的内容是否包含敏感或违规信息
/// </summary>
public static class ContentGuardrailMiddleware
{
    /// <summary>
    /// 敏感词列表（可从配置或数据库加载）
    /// </summary>
    private static readonly HashSet<string> s_blockedTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "暴力", "赌博", "毒品", "色情"
    };

    /// <summary>
    /// 流式拦截需要保留的滑动缓冲长度（取最长敏感词的 2 倍，足够覆盖跨 chunk 拼接）
    /// </summary>
    private static readonly int s_slidingBufferSize = s_blockedTerms.Max(t => t.Length) * 2;

    /// <summary>
    /// 审计日志记录器（启动期通过 <see cref="Configure(ILogger)"/> 注入）
    /// 中间件签名由 MAF 框架固定，无法注入 ILogger，此处采用静态注入
    /// </summary>
    private static ILogger s_logger = NullLogger.Instance;

    /// <summary>
    /// 配置审计日志记录器（建议在应用启动期调用一次）
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public static void Configure(ILogger logger) => s_logger = logger ?? NullLogger.Instance;

    /// <summary>
    /// Agent 级别护栏中间件（非流式）：检查输出内容是否包含敏感词
    /// </summary>
    public static async Task<AgentResponse> InvokeAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken)
    {
        AgentResponse response = await innerAgent.RunAsync(messages, session, options, cancellationToken);

        string responseText = response.Text;

        foreach (string term in s_blockedTerms)
        {
            if (responseText.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                s_logger.LogWarning("[Guardrail] Blocked non-streaming response. Term={Term} Length={Length}",
                    term, responseText.Length);
                return new AgentResponse([new ChatMessage(ChatRole.Assistant,
                    "抱歉，生成的内容包含不适当的信息，已被安全系统拦截。请重新描述您的需求。")]);
            }
        }

        return response;
    }

    /// <summary>
    /// Agent 级别护栏中间件（流式）：跨 chunk 滑动窗口检测敏感词
    /// 旧实现只看单个 chunk 文本，"暴力"被拆成"暴"+"力"会绕过；
    /// 改为维护一个滑动缓冲区，每次拼上新 chunk 后整体匹配一次。
    /// </summary>
    public static async IAsyncEnumerable<AgentResponseUpdate> InvokeStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        StringBuilder slidingBuffer = new();
        bool blocked = false;

        await foreach (AgentResponseUpdate update in innerAgent.RunStreamingAsync(messages, session, options, cancellationToken))
        {
            if (blocked)
            {
                continue;
            }

            string? newText = ExtractText(update);
            if (!string.IsNullOrEmpty(newText))
            {
                slidingBuffer.Append(newText);

                // 先做整体匹配，再裁剪缓冲（避免单次大块输入被裁掉敏感词）
                string window = slidingBuffer.ToString();
                foreach (string term in s_blockedTerms)
                {
                    if (window.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        blocked = true;
                        s_logger.LogWarning("[Guardrail] Blocked streaming response. Term={Term}", term);
                        yield return new AgentResponseUpdate(ChatRole.Assistant,
                            "\n\n[安全] 抱歉，生成的内容包含不适当的信息，已被安全系统拦截。请重新描述您的需求。");
                        break;
                    }
                }

                // 截断 buffer 防止无限增长，但保留尾部足以拼接下一段
                if (slidingBuffer.Length > s_slidingBufferSize)
                {
                    slidingBuffer.Remove(0, slidingBuffer.Length - s_slidingBufferSize);
                }
            }

            if (!blocked)
            {
                yield return update;
            }
        }
    }

    /// <summary>
    /// 抽取一个流式 update 中的文本片段，没有则返回 null
    /// </summary>
    private static string? ExtractText(AgentResponseUpdate update)
    {
        StringBuilder? sb = null;
        foreach (AIContent content in update.Contents)
        {
            if (content is TextContent text && !string.IsNullOrEmpty(text.Text))
            {
                sb ??= new StringBuilder();
                sb.Append(text.Text);
            }
        }

        return sb?.ToString();
    }
}
