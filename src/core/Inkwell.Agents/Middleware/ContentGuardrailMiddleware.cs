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
    /// Agent 级别护栏中间件：检查输出内容是否包含敏感词
    /// </summary>
    public static async Task<AgentResponse> InvokeAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken)
    {
        AgentResponse response = await innerAgent.RunAsync(messages, session, options, cancellationToken);

        // 检查响应文本
        string responseText = response.Text;

        foreach (string term in s_blockedTerms)
        {
            if (responseText.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                // 发现敏感内容，返回安全替代响应
                return new AgentResponse([new ChatMessage(ChatRole.Assistant,
                    "抱歉，生成的内容包含不适当的信息，已被安全系统拦截。请重新描述您的需求。")]);
            }
        }

        return response;
    }
}
