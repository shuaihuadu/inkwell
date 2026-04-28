using Inkwell;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell.Workflows;

/// <summary>
/// Workflow 服务注册扩展方法
/// </summary>
public static class WorkflowServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Inkwell 默认 Workflow 集合
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="primaryChatClient">主模型客户端</param>
    /// <param name="secondaryChatClient">辅助模型客户端</param>
    /// <returns>Workflow 注册表</returns>
    public static WorkflowRegistry AddInkwellWorkflows(
        this IServiceCollection services,
        IChatClient primaryChatClient,
        IChatClient secondaryChatClient)
    {
        WorkflowRegistry registry = new();

        // 文章写入网关：Executor 可在运行时创建临时 Scope 调用 Scoped 的 IArticlePersistenceProvider
        // 网关实例先注册、稍后由宿主在 DI 构建完成后赋值 ScopeFactory
        ArticleWriteGateway articleGateway = services.FindSingletonInstance<ArticleWriteGateway>()
            ?? new ArticleWriteGateway();
        if (services.FindSingletonInstance<ArticleWriteGateway>() is null)
        {
            services.AddSingleton(articleGateway);
        }

        // 1. 内容生产流水线（串行 + 并行 + HITL）
        Workflow contentPipeline = ContentPipelineBuilder.Build(primaryChatClient, articleGateway: articleGateway);
        registry.Register(new WorkflowRegistration
        {
            Id = "content-pipeline",
            Name = "内容生产流水线",
            Description = "选题分析(Fan-Out) → 写作(Writer-Critic Loop) → 人工审核(HITL)",
            Workflow = contentPipeline,
            Capabilities = new WorkflowCapabilities { SupportsHumanInLoop = true },
            Documentation = new WorkflowDocumentation
            {
                Purpose = "从一个粗略的选题想法，一路跑到 写作 → 自审 → 人工审核 通过的成品文章。",
                InputHint = "用一句话写下选题或主题，例如：『AI 在医疗影像诊断的最新进展』",
                InputExample = "AI 在医疗影像诊断的最新进展",
                OutputHint = "产出一篇结构化文章；中途会触发人工审核（HITL），需要在对话中点 通过/退回",
                Tags = ["Fan-Out", "Writer-Critic Loop", "HITL"]
            }
        });

        // 2. 多语言翻译流水线（Fan-Out / Fan-In）
        registry.Register(new WorkflowRegistration
        {
            Id = "translation-pipeline",
            Name = "多语言翻译流水线",
            Description = "一篇文章同时翻译为 English、Japanese、French（Fan-Out / Fan-In）",
            Workflow = TranslationPipelineBuilder.Build(secondaryChatClient),
            Documentation = new WorkflowDocumentation
            {
                Purpose = "把一段中文原文同时翻译成 English / Japanese / French 三个版本，并合并成一份多语言结果。",
                InputHint = "粘贴一段需要翻译的中文原文（一段或多段都行）",
                InputExample = "Microsoft Agent Framework 是一个用于构建、编排和部署 AI Agent 的多语言开源框架，支持串行、并行、循环和人工介入等多种工作流模式。",
                OutputHint = "返回一份合并的 MultiLanguageResult，包含三种语言的翻译版本",
                Tags = ["Fan-Out", "Fan-In"]
            }
        });

        // 3. Writer-Critic 循环（独立 Loop）
        registry.Register(new WorkflowRegistration
        {
            Id = "writer-critic-loop",
            Name = "Writer-Critic 循环",
            Description = "写作-审核迭代循环，直到审核通过或达到最大修订次数",
            Workflow = WriterCriticLoopBuilder.Build(primaryChatClient),
            Documentation = new WorkflowDocumentation
            {
                Purpose = "作家写一稿、评审给意见、作家照着改，直到评审通过或达到上限。",
                InputHint = "输入要写的题目或一段写作要求",
                InputExample = "写一篇 600 字左右的科普短文，介绍向量数据库与传统数据库的差异",
                OutputHint = "返回最终通过评审的稿件；过程中会看到多轮 写 → 审 的迭代记录",
                Tags = ["Loop", "Writer-Critic"]
            }
        });

        // 4. 选题讨论会（GroupChat）
        registry.Register(new WorkflowRegistration
        {
            Id = "topic-discussion",
            Name = "选题讨论会",
            Description = "市场分析师 + 内容编辑 + SEO 专家围绕选题轮流讨论（GroupChat）",
            Workflow = TopicDiscussionBuilder.Build(secondaryChatClient),
            Capabilities = new WorkflowCapabilities
            {
                // GroupChat 入口期望 List<ChatMessage>，需要把用户文本包成一条 User 消息
                InputAdapter = static s => new List<ChatMessage> { new(ChatRole.User, s) },
                // GroupChatHost 是 ChatProtocolExecutor，必须再发一个 TurnToken 才会触发轮次执行
                RequiresTurnToken = true
            },
            Documentation = new WorkflowDocumentation
            {
                Purpose = "市场分析师 / 内容编辑 / SEO 专家三人围绕一个候选选题轮流发言，最终给出是否值得做的结论。",
                InputHint = "给出一个候选选题或方向，让三位角色围绕它进行讨论",
                InputExample = "我们 Q3 想做一篇关于 AI 编程助手 的内容，方向定位是面向后端开发者，请讨论是否合适以及具体角度。",
                OutputHint = "按轮次输出三位角色的发言，最后给出讨论结论",
                Tags = ["GroupChat", "Multi-Agent"]
            }
        });

        // 5. 智能路由（Handoff）
        registry.Register(new WorkflowRegistration
        {
            Id = "smart-routing",
            Name = "智能路由",
            Description = "Coordinator 根据问题类型自动切换到写作/SEO/翻译专家（Handoff）",
            Workflow = SmartRoutingBuilder.Build(primaryChatClient, secondaryChatClient),
            Capabilities = new WorkflowCapabilities
            {
                // Handoff 入口期望 List<ChatMessage>，同 GroupChat 处理
                InputAdapter = static s => new List<ChatMessage> { new(ChatRole.User, s) },
                // HandoffHost 是 ChatProtocolExecutor，必须再发一个 TurnToken 才会触发协调员发言
                RequiresTurnToken = true
            },
            Documentation = new WorkflowDocumentation
            {
                Purpose = "协调员先听完你的诉求，再把任务交给 写作 / SEO / 翻译 中合适的专家来处理。",
                InputHint = "用一句话描述你想做的事，由系统自动选择最合适的专家",
                InputExample = "帮我把这段产品介绍翻译成英文：『Inkwell 是一个面向内容创作团队的 AI 内容生产平台。』",
                OutputHint = "先看到协调员判断把任务给谁，再看到对应专家的最终回答",
                Tags = ["Handoff", "Multi-Agent"]
            }
        });

        // 6. 批量内容评估（MapReduce）
        registry.Register(new WorkflowRegistration
        {
            Id = "batch-evaluation",
            Name = "批量内容评估",
            Description = "对 N 篇文章并行多维度评分，汇总排序（MapReduce 动态 Fan-Out）",
            Workflow = BatchEvaluationBuilder.Build(secondaryChatClient),
            Capabilities = new WorkflowCapabilities
            {
                // 入口期望 List<ArticleEvaluation>；支持两种原始输入：
                //   1) 完整 JSON 数组：[{"title":"...","content":"..."}, ...]
                //   2) 纯文本：每段空行分隔，段首第一行作为标题，其余作为内容
                InputAdapter = static s => BatchEvaluationInputAdapter.Parse(s)
            },
            Documentation = new WorkflowDocumentation
            {
                Purpose = "一次喂入 N 篇文章，并行从多个维度打分、最后按综合分排序。",
                InputHint = "两种格式均可：(1) JSON 数组 [{title, content}, ...]；(2) 每段用空行分隔，段首一行当标题、其余当正文",
                InputExample = "AI 编程助手现状\n本文盘点 2025 年主流 AI 编程助手的能力差异……\n\n向量数据库 101\n本文用最短篇幅讲清楚向量数据库的核心概念……",
                OutputHint = "返回每篇文章的多维度评分 + 综合排名",
                Tags = ["MapReduce", "Fan-Out", "Fan-In"]
            }
        });

        // 7. 内容生产 + 翻译一体化（SubWorkflow）
        registry.Register(new WorkflowRegistration
        {
            Id = "content-with-translation",
            Name = "内容 + 翻译一体化",
            Description = "写作完成后自动触发翻译子工作流（SubWorkflow / BindAsExecutor）",
            Workflow = ContentWithTranslationBuilder.Build(primaryChatClient, secondaryChatClient),
            Documentation = new WorkflowDocumentation
            {
                Purpose = "先按选题写一篇中文文章，紧接着自动把文章翻译成英文 + 日文。",
                InputHint = "输入要写的中文选题，写完后会自动翻译成 English / Japanese",
                InputExample = "为什么团队都在转向 AI 内容工厂",
                OutputHint = "输出包含中文成稿与对应的英文 / 日文译文",
                Tags = ["SubWorkflow", "Pipeline", "Fan-Out"]
            }
        });

        services.AddSingleton(registry);

        return registry;
    }

    /// <summary>
    /// 注册 Inkwell 默认 Workflow 集合（从已注册的 Keyed IChatClient 解析）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>Workflow 注册表</returns>
    public static WorkflowRegistry AddInkwellWorkflows(this IServiceCollection services)
    {
        IChatClient? primaryClient = services.FindKeyedSingletonInstance<IChatClient>(AIProviderKeys.Primary);
        IChatClient? secondaryClient = services.FindKeyedSingletonInstance<IChatClient>(AIProviderKeys.Secondary);

        if (primaryClient is null)
        {
            throw new InvalidOperationException(
                "Primary IChatClient not found. Call UseAIProviders() before AddInkwellWorkflows().");
        }

        secondaryClient ??= primaryClient;

        return services.AddInkwellWorkflows(primaryClient, secondaryClient);
    }
}
