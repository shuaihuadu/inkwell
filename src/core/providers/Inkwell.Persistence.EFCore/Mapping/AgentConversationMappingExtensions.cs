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
            OwnerUserId = entity.OwnerUserId,
            Title = entity.Title,
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
            OwnerUserId = model.OwnerUserId,
            Title = model.Title,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
            RowVersion = model.RowVersion,
        };
    }

    public static IQueryable<AgentSessionDefinition> SelectAsModel(this IQueryable<AgentConversationEntity> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(entity => new AgentSessionDefinition
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            OwnerUserId = entity.OwnerUserId,
            Title = entity.Title,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
            RowVersion = entity.RowVersion,
        });
    }
}
