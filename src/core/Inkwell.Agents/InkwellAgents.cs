using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell.Agents;

/// <summary>
/// Inkwell 预定义 Agent 工厂
/// 创建系统内置的 AI Agent 实例
/// </summary>
public static class InkwellAgents
{
    /// <summary>
    /// 创建内容写手 Agent
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateWriter(IChatClient chatClient)
    {
        AIAgent agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    你是一名专业内容写手。你擅长撰写引人入胜、结构清晰的文章。
                    你的文章信息丰富、对目标受众有吸引力。注重清晰度、叙事性和可操作的见解。
                    请用中文回复。
                    """
            }
        });

        return new AgentRegistration
        {
            Id = "writer",
            Name = "内容写手",
            Description = "撰写高质量文章内容",
            Agent = agent,
            AguiRoute = "/api/agui/writer"
        };
    }

    /// <summary>
    /// 创建内容审核 Agent
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateCritic(IChatClient chatClient)
    {
        AIAgent agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    你是一名严格的内容编辑。从质量、准确性、吸引力和完整性四个维度审核文章。
                    提供建设性的反馈，指出问题并给出改进建议。请用中文回复。
                    """
            }
        });

        return new AgentRegistration
        {
            Id = "critic",
            Name = "内容审核",
            Description = "审核文章质量并提供改进建议",
            Agent = agent,
            AguiRoute = "/api/agui/critic"
        };
    }

    /// <summary>
    /// 创建市场分析 Agent
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateMarketAnalyst(IChatClient chatClient)
    {
        AIAgent agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    你是一名市场研究分析师。分析给定主题的市场趋势、目标受众和内容机会。
                    提供数据驱动的洞察和可操作的建议。请用中文回复。
                    """
            }
        });

        return new AgentRegistration
        {
            Id = "market-analyst",
            Name = "市场分析",
            Description = "分析市场趋势和目标受众",
            Agent = agent,
            AguiRoute = "/api/agui/market-analyst"
        };
    }

    /// <summary>
    /// 创建 SEO 优化 Agent
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateSeoOptimizer(IChatClient chatClient)
    {
        AIAgent agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    你是一名 SEO 优化专家。分析文章的关键词密度、标题优化、元描述等 SEO 指标。
                    提供具体的优化建议，帮助文章在搜索引擎中获得更好的排名。请用中文回复。
                    """
            }
        });

        return new AgentRegistration
        {
            Id = "seo-optimizer",
            Name = "SEO 优化",
            Description = "分析和优化文章的搜索引擎排名",
            Agent = agent,
            AguiRoute = "/api/agui/seo-optimizer"
        };
    }

    /// <summary>
    /// 创建翻译 Agent
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <param name="targetLanguage">目标语言</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateTranslator(IChatClient chatClient, string targetLanguage = "English")
    {
        string agentId = $"translator-{targetLanguage.ToLowerInvariant()}";

        AIAgent agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = $"""
                    你是一名专业翻译。将用户提供的内容准确翻译成{targetLanguage}。
                    保持原文的语气、风格和格式。确保翻译自然流畅，符合目标语言的表达习惯。
                    """
            }
        });

        return new AgentRegistration
        {
            Id = agentId,
            Name = $"翻译（{targetLanguage}）",
            Description = $"将内容翻译成{targetLanguage}",
            Agent = agent,
            AguiRoute = $"/api/agui/{agentId}"
        };
    }
}
