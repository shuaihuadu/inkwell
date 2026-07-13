// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentChatMessageEntity : IHasTimestamps
{
    public Guid Id { get; init; }

    public Guid SessionId { get; init; }

    public string Message { get; init; } = string.Empty;

    public int SequenceNumber { get; init; }

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }

    public AgentSessionEntity? Session { get; init; }
}
