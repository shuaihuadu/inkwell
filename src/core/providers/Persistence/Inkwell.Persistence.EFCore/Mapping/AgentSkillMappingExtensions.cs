// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentSkillMappingExtensions
{
    public static AgentSkillDefinition ToModel(this AgentSkillEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AgentSkillDefinition
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Content = entity.ContentMarkdown,
            ReferenceFileUris = JsonSerializer.Deserialize<IReadOnlyList<Uri>>(entity.ReferenceFileUris) ?? [],
            AssetFileUris = JsonSerializer.Deserialize<IReadOnlyList<Uri>>(entity.AssetFileUris) ?? [],
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static AgentSkillEntity ToEntity(this AgentSkillDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new AgentSkillEntity
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            ContentMarkdown = model.Content,
            ReferenceFileUris = JsonSerializer.Serialize(model.ReferenceFileUris),
            AssetFileUris = JsonSerializer.Serialize(model.AssetFileUris),
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }

    /// <summary>因含 JSON 序列化列，反序列化无法翻译为 SQL；先拉取整行再于客户端投影（同 <see cref="AgentMappingExtensions"/>）。</summary>
    public static IQueryable<AgentSkillDefinition> SelectAsModel(this IQueryable<AgentSkillEntity> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.AsEnumerable().Select(ToModel).AsQueryable();
    }
}
