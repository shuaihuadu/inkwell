// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentMappingExtensions
{
    public static AgentDefinition ToModel(this AgentEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AgentDefinition
        {
            Id = entity.Id,
            OwnerUserId = entity.OwnerUserId,
            CurrentPublishedVersionId = entity.CurrentPublishedVersionId,
            DraftVersionId = entity.DraftVersionId,
            LatestPublishedVersionNumber = entity.LatestPublishedVersionNumber,
            IsShared = entity.IsShared,
            SharedRevokedByAdminTime = entity.SharedRevokedByAdminTime,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
            RowVersion = entity.RowVersion,
        };
    }

    public static AgentEntity ToEntity(this AgentDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new AgentEntity
        {
            Id = model.Id,
            OwnerUserId = model.OwnerUserId,
            CurrentPublishedVersionId = model.CurrentPublishedVersionId,
            DraftVersionId = model.DraftVersionId,
            LatestPublishedVersionNumber = model.LatestPublishedVersionNumber,
            IsShared = model.IsShared,
            SharedRevokedByAdminTime = model.SharedRevokedByAdminTime,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
            RowVersion = model.RowVersion,
        };
    }

    /// <summary>
    /// 将 Agent Entity 投影为业务 Model。
    /// </summary>
    public static IQueryable<AgentDefinition> SelectAsModel(this IQueryable<AgentEntity> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(entity => new AgentDefinition
        {
            Id = entity.Id,
            OwnerUserId = entity.OwnerUserId,
            CurrentPublishedVersionId = entity.CurrentPublishedVersionId,
            DraftVersionId = entity.DraftVersionId,
            LatestPublishedVersionNumber = entity.LatestPublishedVersionNumber,
            IsShared = entity.IsShared,
            SharedRevokedByAdminTime = entity.SharedRevokedByAdminTime,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
            RowVersion = entity.RowVersion,
        });
    }
}
