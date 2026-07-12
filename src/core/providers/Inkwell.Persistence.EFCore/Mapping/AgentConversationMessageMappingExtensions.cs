// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentConversationMessageMappingExtensions
{
    public static AgentChatMessage ToModel(this AgentConversationMessageEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AgentChatMessage
        {
            Id = entity.Id,
            SessionId = entity.ConversationId,
            Message = DeserializeMessage(entity.MessageJson),
            SequenceNumber = entity.SequenceNumber,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static AgentConversationMessageEntity ToEntity(this AgentChatMessage model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new AgentConversationMessageEntity
        {
            Id = model.Id,
            ConversationId = model.SessionId,
            MessageJson = JsonSerializer.Serialize(model.Message),
            SequenceNumber = model.SequenceNumber,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }

    private static ChatMessage DeserializeMessage(string messageJson) =>
        JsonSerializer.Deserialize<ChatMessage>(messageJson)
        ?? throw new JsonException("The persisted chat message JSON deserialized to null.");
}
