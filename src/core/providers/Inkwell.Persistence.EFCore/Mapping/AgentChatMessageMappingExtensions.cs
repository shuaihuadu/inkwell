// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentChatMessageMappingExtensions
{
    public static AgentChatMessage ToModel(this AgentChatMessageEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AgentChatMessage
        {
            Id = entity.Id,
            SessionId = entity.SessionId,
            Message = DeserializeMessage(entity.Message),
            SequenceNumber = entity.SequenceNumber,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static AgentChatMessageEntity ToEntity(this AgentChatMessage model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new AgentChatMessageEntity
        {
            Id = model.Id,
            SessionId = model.SessionId,
            Message = JsonSerializer.Serialize(model.Message),
            SequenceNumber = model.SequenceNumber,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }

    private static ChatMessage DeserializeMessage(string messageJson) =>
        JsonSerializer.Deserialize<ChatMessage>(messageJson)
        ?? throw new JsonException("The persisted chat message JSON deserialized to null.");
}
