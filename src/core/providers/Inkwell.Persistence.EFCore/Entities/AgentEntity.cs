// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentEntity : IHasTimestamps, IHasRowVersion, IHasOwner
{
    public Guid Id { get; init; }

    public Guid OwnerUserId { get; init; }

    public Guid? CurrentPublishedVersionId { get; init; }

    public Guid? DraftVersionId { get; init; }

    public int LatestPublishedVersionNumber { get; init; }

    public bool IsShared { get; init; }

    public DateTimeOffset? SharedRevokedByAdminTime { get; init; }

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
