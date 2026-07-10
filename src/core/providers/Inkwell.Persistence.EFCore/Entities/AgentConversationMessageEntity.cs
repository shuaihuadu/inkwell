// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentConversationMessageEntity : IHasTimestamps
{
    public Guid Id { get; init; }

    public Guid ConversationId { get; init; }

    public string Role { get; init; } = string.Empty;

    public string ContentJson { get; init; } = "[]";

    public string? AuthorName { get; init; }

    public int SequenceNumber { get; init; }

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }

    public AgentConversationEntity? Conversation { get; init; }
}
