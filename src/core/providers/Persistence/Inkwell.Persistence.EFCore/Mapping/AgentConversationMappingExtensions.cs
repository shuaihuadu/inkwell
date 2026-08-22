// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentConversationMappingExtensions
{
    public static AgentConversation ToModel(this AgentConversationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new AgentConversation
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            AgentVersionId = entity.AgentVersionId,
            OwnerUserId = entity.OwnerUserId,
            Title = entity.Title,
            LastActivityTime = entity.LastActivityTime,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static AgentConversationEntity ToEntity(this AgentConversation model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new AgentConversationEntity
        {
            Id = model.Id,
            AgentId = model.AgentId,
            AgentVersionId = model.AgentVersionId,
            OwnerUserId = model.OwnerUserId,
            Title = model.Title,
            LastActivityTime = model.LastActivityTime,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }
}