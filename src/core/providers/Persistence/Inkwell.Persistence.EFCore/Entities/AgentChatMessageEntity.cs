// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentChatMessageEntity : IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string? RunId { get; set; }
    public int? RunMessageIndex { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public DateTimeOffset CreatedTime { get; init; }
    public DateTimeOffset UpdatedTime { get; init; }
    public AgentConversationEntity? Conversation { get; set; }
}
