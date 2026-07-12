// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentVersionMappingExtensions
{
    public static AgentVersion ToModel(this AgentVersionEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        AgentSnapshot snapshot = JsonSerializer.Deserialize<AgentSnapshot>(entity.SnapshotJson)
            ?? throw new JsonException($"Agent version snapshot is null: versionId={entity.Id}");

        return new AgentVersion
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            VersionNumber = entity.VersionNumber,
            Status = entity.Status,
            Snapshot = snapshot,
            CreatedByUserId = entity.CreatedByUserId,
            ChangeSummary = entity.ChangeSummary,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
            PublishedTime = entity.PublishedTime,
            RowVersion = entity.RowVersion,
        };
    }

    public static AgentVersionEntity ToEntity(this AgentVersion model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new AgentVersionEntity
        {
            Id = model.Id,
            AgentId = model.AgentId,
            VersionNumber = model.VersionNumber,
            Status = model.Status,
            SnapshotJson = JsonSerializer.Serialize(model.Snapshot),
            CreatedByUserId = model.CreatedByUserId,
            ChangeSummary = model.ChangeSummary,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
            PublishedTime = model.PublishedTime,
            RowVersion = model.RowVersion,
        };
    }
}