// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentConversationEntity : IHasTimestamps, IHasOwner, IHasRowVersion
{
    public Guid Id { get; init; }

    public Guid AgentId { get; init; }

    public Guid OwnerUserId { get; init; }

    public string? Title { get; init; }

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public List<AgentConversationMessageEntity> Messages { get; init; } = [];
}
