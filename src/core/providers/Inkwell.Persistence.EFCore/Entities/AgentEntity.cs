// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentEntity : IHasTimestamps, IHasOwner
{
    public Guid Id { get; init; }

    public Guid OwnerUserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? AvatarUri { get; init; }

    public string? Description { get; init; }

    public string? Instructions { get; init; }

    public string BuildOptions { get; init; } = string.Empty;

    public Guid? CurrentPublishedVersionId { get; init; }

    public int LatestPublishedVersionNumber { get; init; }

    public bool IsShared { get; init; }

    public DateTimeOffset? SharedRevokedByAdminTime { get; init; }

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }

}
