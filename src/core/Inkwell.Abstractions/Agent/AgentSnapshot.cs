// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Collections.Immutable;

namespace Inkwell;

/// <summary>
/// Agent 版本的不可变运行配置快照；历史版本不得反向读取当前定义的可变字段。
/// </summary>
public sealed record class AgentSnapshot
{
    /// <summary>
    /// 获取 Agent 名称。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 获取 Agent 头像地址。
    /// </summary>
    public Uri? AvatarUri { get; init; }

    /// <summary>
    /// 获取 Agent 描述。
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 获取 Agent 系统指令。
    /// </summary>
    public string? Instructions { get; init; }

    /// <summary>
    /// 获取模型目录中的模型标识；为空时使用 Factory 的默认部署。
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// 获取模型调用参数。
    /// </summary>
    public AgentModelParameters? ModelParameters { get; init; }

    /// <summary>
    /// 获取工具绑定快照。
    /// </summary>
    public ImmutableArray<AgentToolBinding> ToolBindings { get; init; } = [];

    /// <summary>
    /// 获取 Skill 绑定快照。
    /// </summary>
    public ImmutableArray<AgentSkillBinding> SkillBindings { get; init; } = [];

    /// <summary>
    /// 获取聊天历史策略快照。
    /// </summary>
    public AgentChatHistoryOptions? ChatHistoryOptions { get; init; }
}
