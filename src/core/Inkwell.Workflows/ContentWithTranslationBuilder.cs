using Inkwell.Workflows.Executors;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows;

/// <summary>
/// 内容生产 + 翻译一体化流水线构建器
/// 演示 SubWorkflow（BindAsExecutor）：主流水线完成写作后，调用翻译子工作流
/// </summary>
public static class ContentWithTranslationBuilder
{
    /// <summary>
    /// 构建内容生产 + 翻译一体化流水线
    /// </summary>
    /// <param name="primaryChatClient">主模型客户端（写作）</param>
    /// <param name="secondaryChatClient">辅助模型客户端（翻译）</param>
    /// <returns>构建好的 Workflow 实例</returns>
    public static Workflow Build(IChatClient primaryChatClient, IChatClient secondaryChatClient)
    {
        // ========== 创建 Agent ==========

        AIAgent writerAgent = primaryChatClient.AsAIAgent(
            instructions: """
                你是一名专业内容写作者。请根据给定主题撰写一篇 200-300 字的文章。
                只返回文章内容，不要添加任何说明。请用中文回复。
                """);

        // ========== 创建 Executor ==========

        SimpleWriterExecutor writer = new(writerAgent);

        // 构建翻译子工作流并绑定为 Executor
        Workflow translationSubWorkflow = TranslationPipelineBuilder.Build(
            secondaryChatClient, ["English", "Japanese"]);

        ExecutorBinding translationExecutor = translationSubWorkflow.BindAsExecutor("TranslationSubWorkflow");

        // ========== 构建主 Workflow ==========

        /*
         *   拓扑结构：
         *
         *   [输入: 主题(string)]
         *        │
         *        ▼
         *   SimpleWriter → [文章内容]
         *        │
         *        ▼
         *   TranslationSubWorkflow（子工作流，内部 Fan-Out/Fan-In）
         *        │
         *        ▼
         *   [输出: MultiLanguageResult]
         */

        return new WorkflowBuilder(writer)
            .WithName("ContentWithTranslation")
            .WithDescription("Content creation + automatic multi-language translation via SubWorkflow (BindAsExecutor)")
            .AddEdge(writer, translationExecutor)
            .WithOutputFrom(translationExecutor)
            .Build();
    }
}

/// <summary>
/// 简化版写作 Executor（直接将主题写成文章，输出 string）
/// </summary>
[SendsMessage(typeof(string))]
internal sealed class SimpleWriterExecutor(AIAgent agent) : Executor<string>("SimpleWriter")
{
    /// <inheritdoc />
    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        AgentResponse response = await agent.RunAsync(
            $"请撰写一篇关于以下主题的文章：{message}",
            cancellationToken: cancellationToken);

        // 输出原文（供翻译子工作流消费）
        await context.SendMessageAsync(response.Text, cancellationToken: cancellationToken);

        // 同时输出给 Workflow
        await context.YieldOutputAsync(response.Text, cancellationToken: cancellationToken);
    }
}
