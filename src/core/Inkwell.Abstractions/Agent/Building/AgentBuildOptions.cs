// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Collections.Immutable;

namespace Inkwell;

/// <summary>
/// 承载构建 Agent 所需的模型、聊天历史、工具与 Skill 配置。
/// </summary>
public sealed record class AgentBuildOptions
{
    /// <summary>
    /// 获取模型配置。
    /// </summary>
    public required AgentModelOptions ModelOptions { get; init; }

    /// <summary>
    /// 获取聊天历史策略。
    /// </summary>
    public AgentChatHistoryOptions? ChatHistoryOptions { get; init; }

    /// <summary>
    /// 获取工具绑定集合。
    /// </summary>
    public ImmutableArray<AgentToolBinding> ToolBindings { get; init; } = [];

    /// <summary>
    /// 获取 Skill 定义集合。
    /// </summary>
    public ImmutableArray<AgentSkillDefinition> Skills { get; init; } = [];
}
