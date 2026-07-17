// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentSessionStateEntity
{
    public Guid ConversationId { get; set; }
    public string SerializedState { get; set; } = string.Empty;
    public long Revision { get; set; }
    public string? LastRunId { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
    public AgentConversationEntity? Conversation { get; set; }
}