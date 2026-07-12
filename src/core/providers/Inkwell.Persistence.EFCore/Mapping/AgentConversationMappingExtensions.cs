// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentConversationMappingExtensions
{
    public static AgentSessionDefinition ToModel(this AgentConversationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AgentSessionDefinition
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            AgentVersionId = entity.AgentVersionId,
            OwnerUserId = entity.OwnerUserId,
            Title = entity.Title,
            MafSessionState = DeserializeSessionState(entity.MafSessionStateJson),
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
            RowVersion = entity.RowVersion,
        };
    }

    public static AgentConversationEntity ToEntity(this AgentSessionDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new AgentConversationEntity
        {
            Id = model.Id,
            AgentId = model.AgentId,
            AgentVersionId = model.AgentVersionId,
            OwnerUserId = model.OwnerUserId,
            Title = model.Title,
            MafSessionStateJson = model.MafSessionState is JsonElement state
                ? state.GetRawText()
                : null,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
            RowVersion = model.RowVersion,
        };
    }

    private static JsonElement? DeserializeSessionState(string? sessionStateJson) =>
        sessionStateJson is null
            ? null
            : JsonSerializer.Deserialize<JsonElement>(sessionStateJson);
}
