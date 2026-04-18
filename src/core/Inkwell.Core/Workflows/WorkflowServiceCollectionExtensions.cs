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

        // 查找已注册的文章持久化提供程序
        IArticlePersistenceProvider? articleProvider = null;
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(IArticlePersistenceProvider)
                && descriptor.ImplementationInstance is IArticlePersistenceProvider ap)
            {
                articleProvider = ap;
                break;
            }
        }

        // 1. 内容生产流水线（串行 + 并行 + HITL）
        Workflow contentPipeline = ContentPipelineBuilder.Build(primaryChatClient, articleProvider: articleProvider);
        registry.Register(new WorkflowRegistration
        {
            Id = "content-pipeline",
            Name = "内容生产流水线",
            Description = "选题分析(Fan-Out) → 写作(Writer-Critic Loop) → 人工审核(HITL)",
            Workflow = contentPipeline,
            Capabilities = new WorkflowCapabilities { SupportsHumanInLoop = true }
        });

        // 2. 多语言翻译流水线（Fan-Out / Fan-In）
        registry.Register(new WorkflowRegistration
        {
            Id = "translation-pipeline",
            Name = "多语言翻译流水线",
            Description = "一篇文章同时翻译为 English、Japanese、French（Fan-Out / Fan-In）",
            Workflow = TranslationPipelineBuilder.Build(secondaryChatClient)
        });

        // 3. Writer-Critic 循环（独立 Loop）
        registry.Register(new WorkflowRegistration
        {
            Id = "writer-critic-loop",
            Name = "Writer-Critic 循环",
            Description = "写作-审核迭代循环，直到审核通过或达到最大修订次数",
            Workflow = WriterCriticLoopBuilder.Build(primaryChatClient)
        });

        // 4. 选题讨论会（GroupChat）
        registry.Register(new WorkflowRegistration
        {
            Id = "topic-discussion",
            Name = "选题讨论会",
            Description = "市场分析师 + 内容编辑 + SEO 专家围绕选题轮流讨论（GroupChat）",
            Workflow = TopicDiscussionBuilder.Build(secondaryChatClient)
        });

        // 5. 智能路由（Handoff）
        registry.Register(new WorkflowRegistration
        {
            Id = "smart-routing",
            Name = "智能路由",
            Description = "Coordinator 根据问题类型自动切换到写作/SEO/翻译专家（Handoff）",
            Workflow = SmartRoutingBuilder.Build(primaryChatClient, secondaryChatClient)
        });

        // 6. 批量内容评估（MapReduce）
        registry.Register(new WorkflowRegistration
        {
            Id = "batch-evaluation",
            Name = "批量内容评估",
            Description = "对 N 篇文章并行多维度评分，汇总排序（MapReduce 动态 Fan-Out）",
            Workflow = BatchEvaluationBuilder.Build(secondaryChatClient)
        });

        // 7. 内容生产 + 翻译一体化（SubWorkflow）
        registry.Register(new WorkflowRegistration
        {
            Id = "content-with-translation",
            Name = "内容 + 翻译一体化",
            Description = "写作完成后自动触发翻译子工作流（SubWorkflow / BindAsExecutor）",
            Workflow = ContentWithTranslationBuilder.Build(primaryChatClient, secondaryChatClient)
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
        IChatClient? primaryClient = null;
        IChatClient? secondaryClient = null;

        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(IChatClient) && descriptor.IsKeyedService)
            {
                if (descriptor.ServiceKey is string key)
                {
                    if (key == ModelServiceKeys.Primary && descriptor.KeyedImplementationInstance is IChatClient primary)
                    {
                        primaryClient = primary;
                    }
                    else if (key == ModelServiceKeys.Secondary && descriptor.KeyedImplementationInstance is IChatClient secondary)
                    {
                        secondaryClient = secondary;
                    }
                }
            }
        }

        if (primaryClient is null)
        {
            throw new InvalidOperationException(
                "Primary IChatClient not found. Call UseAzureOpenAI() before AddInkwellWorkflows().");
        }

        secondaryClient ??= primaryClient;

        return services.AddInkwellWorkflows(primaryClient, secondaryClient);
    }
}
