using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows;

/// <summary>
/// 智能路由 Handoff 构建器
/// 根据用户问题类型自动切换到对应专业 Agent
/// </summary>
public static class SmartRoutingBuilder
{
    /// <summary>
    /// 构建智能路由 Handoff Workflow
    /// </summary>
    /// <param name="primaryChatClient">主模型客户端</param>
    /// <param name="secondaryChatClient">辅助模型客户端</param>
    /// <returns>构建好的 Workflow 实例</returns>
    public static Workflow Build(IChatClient primaryChatClient, IChatClient secondaryChatClient)
    {
        // ========== 创建参与者 Agent ==========

        ChatClientAgent coordinator = new(
            secondaryChatClient,
            new ChatClientAgentOptions
            {
                Name = "Coordinator",
                Description = "智能调度员，负责接待用户并判断问题类型",
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        你是 Inkwell 内容平台的智能调度员。
                        当用户提出问题时，判断问题类型并转交给对应的专业 Agent：
                        - 写文章、改文章 → 转交给 WriterSpecialist
                        - SEO优化、关键词 → 转交给 SeoSpecialist
                        - 翻译内容 → 转交给 TranslatorSpecialist
                        - 其他问题 → 自己回答
                        请用中文回复。
                        """
                }
            });

        ChatClientAgent writerSpecialist = new(
            primaryChatClient,
            new ChatClientAgentOptions
            {
                Name = "WriterSpecialist",
                Description = "写作专家，处理内容创作相关问题",
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        你是一名专业内容写手。处理用户的写作需求：撰写文章、修改文章、提供写作建议。
                        完成后告知用户可以继续提问或回到主菜单。请用中文回复。
                        """
                }
            });

        ChatClientAgent seoSpecialist = new(
            secondaryChatClient,
            new ChatClientAgentOptions
            {
                Name = "SeoSpecialist",
                Description = "SEO 专家，处理搜索优化相关问题",
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        你是一名 SEO 优化专家。处理用户的 SEO 需求：关键词优化、标题优化、元描述建议。
                        完成后告知用户可以继续提问或回到主菜单。请用中文回复。
                        """
                }
            });

        ChatClientAgent translatorSpecialist = new(
            secondaryChatClient,
            new ChatClientAgentOptions
            {
                Name = "TranslatorSpecialist",
                Description = "翻译专家，处理内容翻译相关问题",
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        你是一名专业翻译。处理用户的翻译需求：中英互译、多语言翻译。
                        完成后告知用户可以继续提问或回到主菜单。请用中文回复。
                        """
                }
            });

        // ========== 构建 Handoff Workflow ==========

        /*
         *   Handoff 拓扑：
         *
         *   [用户输入]
         *        │
         *        ▼
         *   Coordinator ─── 写作问题 ──→ WriterSpecialist ──┐
         *       │                                            │
         *       ├── SEO 问题 ──→ SeoSpecialist ──────────────┤ (可返回)
         *       │                                            │
         *       └── 翻译问题 ──→ TranslatorSpecialist ───────┘
         */

        return AgentWorkflowBuilder
            .CreateHandoffBuilderWith(coordinator)
            .WithHandoff(coordinator, writerSpecialist, "用户需要写作或修改文章时转交")
            .WithHandoff(coordinator, seoSpecialist, "用户需要 SEO 优化或关键词分析时转交")
            .WithHandoff(coordinator, translatorSpecialist, "用户需要翻译内容时转交")
            .EnableReturnToPrevious()
            .Build();
    }
}
