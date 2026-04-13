using Inkwell.Agents.Middleware;
using Inkwell.Agents.Skills;
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
    /// 默认对话历史保留消息数
    /// </summary>
    private const int DefaultChatHistoryRetainCount = 20;

    /// <summary>
    /// 创建内容写手 Agent（带搜索工具、ChatReducer、护栏 + 审计中间件）
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <param name="chatHistoryRetainCount">对话历史保留数量，默认 20</param>
    /// <param name="contextProviders">额外的 AI 上下文提供程序（如 RAG TextSearchProvider）</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateWriter(
        IChatClient chatClient,
        int chatHistoryRetainCount = DefaultChatHistoryRetainCount,
        IEnumerable<AIContextProvider>? contextProviders = null)
    {
        List<AIContextProvider> providers = contextProviders is not null ? [.. contextProviders] : [];

        AIAgent baseAgent = chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "Writer",
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    你是一名专业内容写手。你擅长撰写引人入胜、结构清晰的文章。
                    你的文章信息丰富、对目标受众有吸引力。注重清晰度、叙事性和可操作的见解。
                    你可以使用搜索工具获取最新资讯来丰富文章内容。
                    你也可以使用 Markdown 校验、可读性分析、敏感词扫描等技能来检查和改进文章。
                    系统会自动从知识库中检索与你写作主题相关的参考资料，请善加利用。
                    请用中文回复。
                    """,
                Tools =
                [
                    AIFunctionFactory.Create(InkwellTools.SearchLatestNews),
                    AIFunctionFactory.Create(MarkdownLintSkill.Lint),
                    AIFunctionFactory.Create(ReadabilitySkill.Analyze),
                    AIFunctionFactory.Create(SensitiveWordSkill.Scan)
                ]
            },
            // 对话历史保留数从参数读取
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new()
            {
                ChatReducer = new MessageCountingChatReducer(chatHistoryRetainCount)
            }),
            // RAG 知识检索（如果配置了 TextSearchProvider）
            AIContextProviders = providers.Count > 0 ? providers : null
        });

        // 应用中间件管线：护栏（同时提供非流式和流式实现）
        AIAgent agent = baseAgent
            .AsBuilder()
            .Use(ContentGuardrailMiddleware.InvokeAsync, ContentGuardrailMiddleware.InvokeStreamingAsync)
            .Build();

        return new AgentRegistration
        {
            Id = "writer",
            Name = "内容写手",
            Description = "撰写高质量文章内容，可调用搜索工具获取最新资讯",
            Agent = agent,
            AguiRoute = "/api/agui/writer"
        };
    }

    /// <summary>
    /// 创建内容审核 Agent（带护栏中间件）
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateCritic(IChatClient chatClient)
    {
        AIAgent baseAgent = chatClient.AsAIAgent(
            name: "Critic",
            instructions: """
                你是一名严格的内容编辑。从质量、准确性、吸引力和完整性四个维度审核文章。
                提供建设性的反馈，指出问题并给出改进建议。请用中文回复。
                """);

        AIAgent agent = baseAgent
            .AsBuilder()
            .Use(ContentGuardrailMiddleware.InvokeAsync, ContentGuardrailMiddleware.InvokeStreamingAsync)
            .Build();

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
    /// 创建市场分析 Agent（结构化输出 TopicAnalysis）
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateMarketAnalyst(IChatClient chatClient)
    {
        AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    你是一名市场研究分析师。分析给定主题的市场趋势、目标受众和内容机会。
                    提供数据驱动的洞察和可操作的建议。请用中文回复。
                    当用户要求结构化分析时，请按照指定的 JSON 格式返回结果。
                    """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<TopicAnalysis>()
            }
        });

        return new AgentRegistration
        {
            Id = "market-analyst",
            Name = "市场分析",
            Description = "分析市场趋势和目标受众，支持结构化输出",
            Agent = agent,
            AguiRoute = "/api/agui/market-analyst"
        };
    }

    /// <summary>
    /// 创建竞品分析 Agent
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateCompetitorAnalyst(IChatClient chatClient)
    {
        AIAgent agent = chatClient.AsAIAgent(
            instructions: """
                你是一名竞品分析专家。研究和分析竞争对手的内容策略、发布频率、话题选择和受众互动。
                识别竞品的优势和劣势，找出差异化机会。提供可操作的竞争策略建议。
                请用中文回复。
                """,
            tools: [AIFunctionFactory.Create(InkwellTools.SearchLatestNews)]);

        return new AgentRegistration
        {
            Id = "competitor-analyst",
            Name = "竞品分析",
            Description = "分析竞品内容策略，发现差异化机会",
            Agent = agent,
            AguiRoute = "/api/agui/competitor-analyst"
        };
    }

    /// <summary>
    /// 创建 SEO 优化 Agent（带关键词分析工具，结构化输出 SeoReport）
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateSeoOptimizer(IChatClient chatClient)
    {
        AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    你是一名 SEO 优化专家。分析文章的关键词密度、标题优化、元描述等 SEO 指标。
                    你可以使用关键词分析工具查询搜索量和竞争度。
                    提供具体的优化建议，帮助文章在搜索引擎中获得更好的排名。
                    当用户要求结构化分析时，请按照指定的 JSON 格式返回结果。请用中文回复。
                    """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<SeoReport>(),
                Tools = [AIFunctionFactory.Create(InkwellTools.AnalyzeKeyword)]
            }
        });

        return new AgentRegistration
        {
            Id = "seo-optimizer",
            Name = "SEO 优化",
            Description = "分析和优化文章的搜索引擎排名，可调用关键词分析工具",
            Agent = agent,
            AguiRoute = "/api/agui/seo-optimizer"
        };
    }

    /// <summary>
    /// 创建图片分析 Agent
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateImageAnalyst(IChatClient chatClient)
    {
        AIAgent agent = chatClient.AsAIAgent(
            instructions: """
                你是一名图片内容分析专家。你可以分析用户上传的图片，生成以下内容：
                1. 图片描述：准确描述图片的内容、构图和氛围
                2. ALT 标签：为网页无障碍访问生成简洁的 ALT 文本
                3. 配图说明：为文章配图撰写合适的说明文字
                4. 标签建议：为图片推荐分类标签
                请用中文回复。
                """);

        return new AgentRegistration
        {
            Id = "image-analyst",
            Name = "图片分析",
            Description = "分析图片内容，生成描述、ALT 标签和配图说明",
            Agent = agent,
            AguiRoute = "/api/agui/image-analyst"
        };
    }

    /// <summary>
    /// 创建智能调度 Agent（Coordinator，带发布审批工具 + SEO Agent 作为函数工具）
    /// </summary>
    /// <param name="chatClient">LLM 客户端</param>
    /// <param name="seoAgent">SEO Agent 实例，包装为函数工具供 Coordinator 调用</param>
    /// <returns>Agent 注册信息</returns>
    public static AgentRegistration CreateCoordinator(IChatClient chatClient, AIAgent? seoAgent = null)
    {
        List<AITool> tools =
        [
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(InkwellTools.PublishArticle))
        ];

        // Agent-as-Tool (2.14)：将 SEO Agent 包装为函数工具
        if (seoAgent is not null)
        {
            tools.Add(seoAgent.AsAIFunction());
        }

        AIAgent agent = chatClient.AsAIAgent(
            instructions: """
                你是 Inkwell 内容平台的智能助手。你的职责是：
                1. 接待用户，理解用户的需求
                2. 根据需求类型引导用户使用合适的功能：
                   - 写文章 → 建议使用「内容写手」Agent
                   - 审核文章 → 建议使用「内容审核」Agent
                   - 分析市场/选题 → 建议使用「市场分析」Agent
                   - 分析竞品 → 建议使用「竞品分析」Agent
                   - SEO 优化 → 可直接使用内置的 SEO 分析工具
                   - 翻译内容 → 建议使用对应语言的翻译 Agent
                   - 分析图片 → 建议使用「图片分析」Agent
                3. 回答关于平台功能的一般性问题
                4. 在用户确认后，可以调用发布工具发布文章
                请用中文回复。保持友好专业的对话风格。
                """,
            tools: tools);

        return new AgentRegistration
        {
            Id = "coordinator",
            Name = "智能调度",
            Description = "接待用户并路由到对应的专业 Agent，可执行文章发布（需审批）",
            Agent = agent,
            AguiRoute = "/api/agui/coordinator"
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

        AIAgent agent = chatClient.AsAIAgent(
            instructions: $"""
                你是一名专业翻译。将用户提供的内容准确翻译成{targetLanguage}。
                保持原文的语气、风格和格式。确保翻译自然流畅，符合目标语言的表达习惯。
                """);

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
