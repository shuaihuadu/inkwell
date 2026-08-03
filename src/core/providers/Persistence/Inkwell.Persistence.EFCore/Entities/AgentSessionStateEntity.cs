// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentSessionStateEntity : IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string SessionState { get; set; } = string.Empty;
    public DateTimeOffset CreatedTime { get; init; }
    public DateTimeOffset UpdatedTime { get; init; }
    public AgentConversationEntity? Conversation { get; set; }
}
