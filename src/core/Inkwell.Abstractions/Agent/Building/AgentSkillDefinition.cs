// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>Skill 目录业务 Model；用 Definition 后缀撞名降级——注意 Microsoft.Agents.AI 命名空间下已存在 <c>AgentSkill</c> 抽象基类，本类型故意不叫 AgentSkill 以避免同名冲突。</summary>
public sealed record class AgentSkillDefinition : IHasTimestamps
{
    /// <summary>
    /// 获取 Skill 标识。
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// 获取 Skill 名称。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 获取 Skill 描述。
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 获取 Skill 主体内容。
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// 获取 Skill 引用文件的存储 URI 集合。
    /// </summary>
    public IReadOnlyList<Uri> ReferenceFileUris { get; init; } = [];

    /// <summary>
    /// 获取 Skill 资源文件的存储 URI 集合。
    /// </summary>
    public IReadOnlyList<Uri> AssetFileUris { get; init; } = [];

    /// <summary>
    /// 获取 Skill 创建时间。
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>
    /// 获取 Skill 更新时间。
    /// </summary>
    public required DateTimeOffset UpdatedTime { get; init; }
}
