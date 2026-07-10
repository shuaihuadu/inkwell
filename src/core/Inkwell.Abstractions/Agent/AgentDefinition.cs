// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>Agent 定义业务 Model；撞名降级（与 <c>Microsoft.Agents.AI.AIAgent</c> 区分）。</summary>
public sealed record class AgentDefinition : IHasOwner, IHasTimestamps, IHasRowVersion
{
    public required Guid Id { get; init; }

    public required Guid OwnerUserId { get; init; }

    public required string Name { get; init; }

    public Uri? AvatarUri { get; init; }

    public string? Description { get; init; }

    public string? Instructions { get; init; }

    public string? ModelId { get; init; }

    public AgentModelParameters? ModelParameters { get; init; }

    public IReadOnlyList<AgentToolBinding> ToolBindings { get; init; } = [];

    public IReadOnlyList<AgentSkillBinding> SkillBindings { get; init; } = [];

    public bool IsShared { get; init; }

    public DateTimeOffset? SharedRevokedByAdminTime { get; init; }

    public int CurrentVersion { get; init; } = 1;

    public required DateTimeOffset CreatedTime { get; init; }

    public required DateTimeOffset UpdatedTime { get; init; }

    public byte[] RowVersion { get; init; } = [];
}

public sealed record class AgentToolBinding(Guid ToolId, string? ParametersJson);

public sealed record class AgentSkillBinding(Guid SkillId);
