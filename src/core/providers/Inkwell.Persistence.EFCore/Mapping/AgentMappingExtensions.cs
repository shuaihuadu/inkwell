// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentMappingExtensions
{
    public static AgentDefinition ToModel(this AgentEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        AgentBuildOptions buildOptions = JsonSerializer.Deserialize<AgentBuildOptions>(entity.BuildOptions)
            ?? throw new JsonException($"Agent build options are null: agentId={entity.Id}");

        return new AgentDefinition
        {
            Id = entity.Id,
            OwnerUserId = entity.OwnerUserId,
            Name = entity.Name,
            AvatarUri = entity.AvatarUri is null ? null : new Uri(entity.AvatarUri, UriKind.RelativeOrAbsolute),
            Description = entity.Description,
            Instructions = entity.Instructions,
            BuildOptions = buildOptions,
            CurrentPublishedVersionId = entity.CurrentPublishedVersionId,
            LatestPublishedVersionNumber = entity.LatestPublishedVersionNumber,
            IsShared = entity.IsShared,
            SharedRevokedByAdminTime = entity.SharedRevokedByAdminTime,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static AgentEntity ToEntity(this AgentDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new AgentEntity
        {
            Id = model.Id,
            OwnerUserId = model.OwnerUserId,
            Name = model.Name,
            AvatarUri = model.AvatarUri?.ToString(),
            Description = model.Description,
            Instructions = model.Instructions,
            BuildOptions = JsonSerializer.Serialize(model.BuildOptions),
            CurrentPublishedVersionId = model.CurrentPublishedVersionId,
            LatestPublishedVersionNumber = model.LatestPublishedVersionNumber,
            IsShared = model.IsShared,
            SharedRevokedByAdminTime = model.SharedRevokedByAdminTime,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }

}
