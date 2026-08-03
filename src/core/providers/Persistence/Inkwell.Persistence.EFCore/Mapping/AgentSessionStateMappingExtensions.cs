// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentSessionStateMappingExtensions
{
    public static AgentSessionState ToModel(this AgentSessionStateEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new AgentSessionState
        {
            Id = entity.Id,
            ConversationId = entity.ConversationId,
            SessionState = entity.SessionState,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static AgentSessionStateEntity ToEntity(this AgentSessionState model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new AgentSessionStateEntity
        {
            Id = model.Id,
            ConversationId = model.ConversationId,
            SessionState = model.SessionState,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }
}
