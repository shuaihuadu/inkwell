// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentSessionStateMappingExtensions
{
    public static AgentSessionState ToModel(this AgentSessionStateEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new AgentSessionState
        {
            ConversationId = entity.ConversationId,
            SerializedState = JsonDocument.Parse(entity.SerializedState).RootElement.Clone(),
            Revision = entity.Revision,
            LastRunId = entity.LastRunId,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static AgentSessionStateEntity ToEntity(this AgentSessionState model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new AgentSessionStateEntity
        {
            ConversationId = model.ConversationId,
            SerializedState = model.SerializedState.GetRawText(),
            Revision = model.Revision,
            LastRunId = model.LastRunId,
            UpdatedTime = model.UpdatedTime,
        };
    }
}