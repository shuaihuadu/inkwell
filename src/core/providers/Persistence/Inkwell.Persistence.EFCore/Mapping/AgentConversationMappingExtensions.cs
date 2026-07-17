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
            SessionKey = entity.SessionKey,
            AgentId = entity.AgentId,
            AgentVersionId = entity.AgentVersionId,
            OwnerUserId = entity.OwnerUserId,
            Title = entity.Title,
            LastCommittedRunId = entity.LastCommittedRunId,
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
            SessionKey = model.SessionKey,
            AgentId = model.AgentId,
            AgentVersionId = model.AgentVersionId,
            OwnerUserId = model.OwnerUserId,
            Title = model.Title,
            LastCommittedRunId = model.LastCommittedRunId,
            LastActivityTime = model.LastActivityTime,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }
}