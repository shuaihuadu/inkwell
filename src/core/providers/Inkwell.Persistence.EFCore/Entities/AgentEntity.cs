using Inkwell;

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentEntity : IHasTimestamps, IHasRowVersion, IHasOwner
{
    public Guid Id { get; init; }

    public Guid OwnerUserId { get; init; }

    public string Name { get; init; } = "";

    public string? AvatarUri { get; init; }

    public string? Description { get; init; }

    public string? Instructions { get; init; }

    public string? ModelId { get; init; }

    /// <summary>序列化的 <c>AgentModelParameters</c>；<c>null</c> = 未设置（使用默认）。</summary>
    public string? ModelParametersJson { get; init; }

    /// <summary>序列化的 <c>IReadOnlyList&lt;AgentToolBinding&gt;</c>；默认空数组。</summary>
    public string ToolBindingsJson { get; init; } = "[]";

    /// <summary>序列化的 <c>IReadOnlyList&lt;AgentSkillBinding&gt;</c>；默认空数组。</summary>
    public string SkillBindingsJson { get; init; } = "[]";

    public bool IsShared { get; init; }

    public DateTimeOffset? SharedRevokedByAdminTime { get; init; }

    public int CurrentVersion { get; init; } = 1;

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
