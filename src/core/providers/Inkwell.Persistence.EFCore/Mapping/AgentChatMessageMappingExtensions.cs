// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentChatMessageMappingExtensions
{
    public static AgentChatMessage ToModel(this AgentChatMessageEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AgentChatMessage
        {
            Id = entity.Id,
            ConversationId = entity.ConversationId,
            RunId = entity.RunId,
            RunMessageIndex = entity.RunMessageIndex,
            Message = AgentChatMessageSerializer.Deserialize(entity.Message),
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
            ConversationId = model.ConversationId,
            RunId = model.RunId,
            RunMessageIndex = model.RunMessageIndex,
            Message = AgentChatMessageSerializer.Serialize(model.Message),
            SequenceNumber = model.SequenceNumber,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }

}
