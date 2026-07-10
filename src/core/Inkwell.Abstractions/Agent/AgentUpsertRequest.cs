// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="IAgentService.CreateAgentAsync"/> / <see cref="IAgentService.UpdateAgentAsync"/> 的请求 DTO。</summary>
public sealed record class AgentUpsertRequest
{
    public required string Name { get; init; }

    public Uri? AvatarUri { get; init; }

    public string? Description { get; init; }

    public string? Instructions { get; init; }

    public string? ModelId { get; init; }

    public AgentModelParameters? ModelParameters { get; init; }

    public IReadOnlyList<AgentToolBinding>? ToolBindings { get; init; }

    public IReadOnlyList<AgentSkillBinding>? SkillBindings { get; init; }
}
