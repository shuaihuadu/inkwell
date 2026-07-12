// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="IAgentService.CreateAgentAsync"/> / <see cref="IAgentService.UpdateAgentAsync"/> 的请求 DTO。</summary>
public sealed record class AgentUpsertRequest
{
    /// <summary>
    /// 获取 Agent 名称。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 获取 Agent 头像 URI。
    /// </summary>
    public Uri? AvatarUri { get; init; }

    /// <summary>
    /// 获取 Agent 描述。
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 获取 Agent 指令。
    /// </summary>
    public string? Instructions { get; init; }

    /// <summary>
    /// 获取模型标识。
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// 获取模型调用参数。
    /// </summary>
    public AgentModelParameters? ModelParameters { get; init; }

    /// <summary>
    /// 获取工具绑定集合。
    /// </summary>
    public IReadOnlyList<AgentToolBinding>? ToolBindings { get; init; }

    /// <summary>
    /// 获取 Skill 绑定集合。
    /// </summary>
    public IReadOnlyList<AgentSkillBinding>? SkillBindings { get; init; }
}
