using Inkwell;
using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentToolMappingExtensions
{
    public static AgentToolDefinition ToModel(this AgentToolEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AgentToolDefinition
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ParametersJsonSchema = entity.ParametersJsonSchema,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static AgentToolEntity ToEntity(this AgentToolDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new AgentToolEntity
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            ParametersJsonSchema = model.ParametersJsonSchema,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }

    public static IQueryable<AgentToolDefinition> SelectAsModel(this IQueryable<AgentToolEntity> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(entity => new AgentToolDefinition
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ParametersJsonSchema = entity.ParametersJsonSchema,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        });
    }
}
