// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentVersionEntity : IHasTimestamps, IHasRowVersion
{
    public Guid Id { get; init; }

    public Guid AgentId { get; init; }

    public int VersionNumber { get; init; }

    public AgentVersionStatus Status { get; init; }

    public string SnapshotJson { get; init; } = string.Empty;

    public Guid CreatedByUserId { get; init; }

    public string? ChangeSummary { get; init; }

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }

    public DateTimeOffset? PublishedTime { get; init; }

    public byte[] RowVersion { get; init; } = [];
}