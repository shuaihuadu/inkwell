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

        // 1. 内容生产流水线（串行 + 并行 + HITL）
        registry.Register(new WorkflowRegistration
        {
            Id = "content-pipeline",
            Name = "内容生产流水线",
            Description = "完整的内容生产流水线：选题分析(Fan-Out) → 写作(Writer-Critic Loop) → 人工审核(HITL)",
            Workflow = ContentPipelineBuilder.Build(primaryChatClient)
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
