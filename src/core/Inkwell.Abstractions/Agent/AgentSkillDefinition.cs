// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>Skill 目录业务 Model；用 Definition 后缀撞名降级——注意 Microsoft.Agents.AI 命名空间下已存在 <c>AgentSkill</c> 抽象基类，本类型故意不叫 AgentSkill 以避免同名冲突。</summary>
public sealed record class AgentSkillDefinition : IHasTimestamps
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string Content { get; init; }

    public IReadOnlyList<Uri> ReferenceFileUris { get; init; } = [];

    public IReadOnlyList<Uri> AssetFileUris { get; init; } = [];

    public required DateTimeOffset CreatedTime { get; init; }

    public required DateTimeOffset UpdatedTime { get; init; }
}
