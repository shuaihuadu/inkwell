// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentConversationEntity : IHasTimestamps, IHasOwner
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid AgentVersionId { get; set; }
    public Guid OwnerUserId { get; init; }
    public string? Title { get; set; }
    public DateTimeOffset LastActivityTime { get; set; }
    public DateTimeOffset CreatedTime { get; init; }
    public DateTimeOffset UpdatedTime { get; init; }
    public List<AgentChatMessageEntity> Messages { get; set; } = [];
}
