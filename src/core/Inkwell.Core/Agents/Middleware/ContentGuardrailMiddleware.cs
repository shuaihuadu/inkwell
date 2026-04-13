using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

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
                return new AgentResponse([new ChatMessage(ChatRole.Assistant,
                    "抱歉，生成的内容包含不适当的信息，已被安全系统拦截。请重新描述您的需求。")]);
            }
        }

        return response;
    }

    /// <summary>
    /// Agent 级别护栏中间件（流式）：透传流式输出，同时检查是否包含敏感词
    /// 如果检测到敏感词，后续更新将被替换为拦截提示
    /// </summary>
    public static async IAsyncEnumerable<AgentResponseUpdate> InvokeStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        bool blocked = false;

        await foreach (AgentResponseUpdate update in innerAgent.RunStreamingAsync(messages, session, options, cancellationToken))
        {
            if (blocked)
            {
                continue;
            }

            // 检查当前 update 的文本内容
            foreach (AIContent content in update.Contents)
            {
                if (content is TextContent textContent && !string.IsNullOrEmpty(textContent.Text))
                {
                    foreach (string term in s_blockedTerms)
                    {
                        if (textContent.Text.Contains(term, StringComparison.OrdinalIgnoreCase))
                        {
                            blocked = true;
                            yield return new AgentResponseUpdate(ChatRole.Assistant,
                                "\n\n⚠️ 抱歉，生成的内容包含不适当的信息，已被安全系统拦截。请重新描述您的需求。");
                            break;
                        }
                    }
                }
            }

            if (!blocked)
            {
                yield return update;
            }
        }
    }
}
