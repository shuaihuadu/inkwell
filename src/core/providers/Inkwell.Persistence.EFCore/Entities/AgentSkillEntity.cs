// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentSkillEntity : IHasTimestamps
{
    public Guid Id { get; init; }

    public string Name { get; init; } = "";

    public string Description { get; init; } = "";

    public string ContentMarkdown { get; init; } = "";

    /// <summary>序列化的 <c>IReadOnlyList&lt;Uri&gt;</c>；默认空数组。</summary>
    public string ReferenceFileUrisJson { get; init; } = "[]";

    /// <summary>序列化的 <c>IReadOnlyList&lt;Uri&gt;</c>；默认空数组。</summary>
    public string AssetFileUrisJson { get; init; } = "[]";

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }
}
