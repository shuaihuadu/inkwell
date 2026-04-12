using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows;

/// <summary>
/// 选题讨论会 GroupChat 构建器
/// 多角色 Agent 围绕选题轮流发言讨论
/// </summary>
public static class TopicDiscussionBuilder
{
    /// <summary>
    /// 构建选题讨论会 GroupChat Workflow
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <param name="maxRounds">最大讨论轮次</param>
    /// <returns>构建好的 Workflow 实例</returns>
    public static Workflow Build(IChatClient chatClient, int maxRounds = 10)
    {
        // ========== 创建参与者 Agent ==========

        ChatClientAgent marketAnalyst = new(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "MarketAnalyst",
                Description = "市场分析师，从市场角度评估选题",
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        你是一名市场分析师，参加选题讨论会。
                        从市场趋势、用户需求、流量潜力角度评估当前讨论的选题。
                        简短发言（100字以内），给出你的观点和评分（1-10分）。请用中文回复。
                        """
                }
            });

        ChatClientAgent contentEditor = new(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "ContentEditor",
                Description = "内容编辑，从内容质量角度评估选题",
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        你是一名资深内容编辑，参加选题讨论会。
                        从内容深度、原创性、可写性角度评估当前讨论的选题。
                        简短发言（100字以内），给出你的观点和评分（1-10分）。请用中文回复。
                        """
                }
            });

        ChatClientAgent seoExpert = new(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "SeoExpert",
                Description = "SEO 专家，从搜索优化角度评估选题",
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        你是一名 SEO 专家，参加选题讨论会。
                        从搜索量、竞争度、长尾词机会角度评估当前讨论的选题。
                        简短发言（100字以内），给出你的观点和评分（1-10分）。请用中文回复。
                        """
                }
            });

        // ========== 创建 GroupChat Manager ==========

        TopicDiscussionManager manager = new([marketAnalyst, contentEditor, seoExpert])
        {
            MaximumIterationCount = maxRounds
        };

        // ========== 构建 Workflow ==========

        /*
         *   GroupChat 拓扑：
         *
         *   [输入: 选题主题]
         *        │
         *        ▼
         *   ┌─ Manager ─────────────────┐
         *   │  轮次 1 → MarketAnalyst   │
         *   │  轮次 2 → ContentEditor   │
         *   │  轮次 3 → SeoExpert       │
         *   │  轮次 4 → MarketAnalyst   │
         *   │  ...（循环直到达成共识）   │
         *   └───────────────────────────┘
         *        │
         *        ▼
         *   [输出: 讨论结论]
         */

        return AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(_ => manager)
            .AddParticipants(marketAnalyst, contentEditor, seoExpert)
            .Build();
    }
}

/// <summary>
/// 选题讨论会 Manager
/// 控制发言顺序：按轮次循环分配给各参与者
/// </summary>
internal sealed class TopicDiscussionManager(AIAgent[] agents) : GroupChatManager
{
    private readonly AIAgent[] _agents = agents;

    /// <inheritdoc />
    protected override ValueTask<AIAgent> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (this._agents.Length == 0)
        {
            throw new InvalidOperationException("TopicDiscussionManager requires at least one agent.");
        }

        // 按轮次循环分配：Market → Editor → SEO → Market → ...
        int agentIndex = this.IterationCount % this._agents.Length;
        return new ValueTask<AIAgent>(this._agents[agentIndex]);
    }
}
