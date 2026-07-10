// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json;
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
            Name = entity.Name,
            AvatarUri = entity.AvatarUri is null ? null : new Uri(entity.AvatarUri),
            Description = entity.Description,
            Instructions = entity.Instructions,
            ModelId = entity.ModelId,
            ModelParameters = entity.ModelParametersJson is null ? null : JsonSerializer.Deserialize<AgentModelParameters>(entity.ModelParametersJson),
            ToolBindings = JsonSerializer.Deserialize<IReadOnlyList<AgentToolBinding>>(entity.ToolBindingsJson) ?? [],
            SkillBindings = JsonSerializer.Deserialize<IReadOnlyList<AgentSkillBinding>>(entity.SkillBindingsJson) ?? [],
            IsShared = entity.IsShared,
            SharedRevokedByAdminTime = entity.SharedRevokedByAdminTime,
            CurrentVersion = entity.CurrentVersion,
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
            Name = model.Name,
            AvatarUri = model.AvatarUri?.ToString(),
            Description = model.Description,
            Instructions = model.Instructions,
            ModelId = model.ModelId,
            ModelParametersJson = model.ModelParameters is null ? null : JsonSerializer.Serialize(model.ModelParameters),
            ToolBindingsJson = JsonSerializer.Serialize(model.ToolBindings),
            SkillBindingsJson = JsonSerializer.Serialize(model.SkillBindings),
            IsShared = model.IsShared,
            SharedRevokedByAdminTime = model.SharedRevokedByAdminTime,
            CurrentVersion = model.CurrentVersion,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
            RowVersion = model.RowVersion,
        };
    }

    /// <summary>
    /// 因 <see cref="AgentEntity"/> 含多个 JSON 序列化列，反序列化无法翻译为 SQL；
    /// 先由 SQL 侧拉取整行，再于客户端完成 <see cref="ToModel"/> 投影（已知的 EFCore JSON 列限制，非 client-eval 误用）。
    /// </summary>
    public static IQueryable<AgentDefinition> SelectAsModel(this IQueryable<AgentEntity> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.AsEnumerable().Select(ToModel).AsQueryable();
    }
}
