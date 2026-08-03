// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentVersionEntity : IHasTimestamps
{
    public Guid Id { get; init; }

    public Guid AgentId { get; init; }

    public int VersionNumber { get; init; }

    public string Snapshot { get; init; } = string.Empty;

    public Guid OwnerUserId { get; init; }

    public string? ChangeSummary { get; init; }

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }

    public DateTimeOffset? PublishedTime { get; init; }

    public UserEntity? OwnerUser { get; init; }

}
