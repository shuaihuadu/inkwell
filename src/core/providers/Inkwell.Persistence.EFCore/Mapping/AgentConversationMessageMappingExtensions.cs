using Microsoft.Extensions.AI;
using Inkwell;
using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentConversationMessageMappingExtensions
{
    public static AgentConversationMessage ToModel(this AgentConversationMessageEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AgentConversationMessage
        {
            Id = entity.Id,
            ConversationId = entity.ConversationId,
            Role = new ChatRole(entity.Role),
            ContentJson = entity.ContentJson,
            AuthorName = entity.AuthorName,
            SequenceNumber = entity.SequenceNumber,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static AgentConversationMessageEntity ToEntity(this AgentConversationMessage model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new AgentConversationMessageEntity
        {
            Id = model.Id,
            ConversationId = model.ConversationId,
            Role = model.Role.Value,
            ContentJson = model.ContentJson,
            AuthorName = model.AuthorName,
            SequenceNumber = model.SequenceNumber,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }

    public static IQueryable<AgentConversationMessage> SelectAsModel(this IQueryable<AgentConversationMessageEntity> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(entity => new AgentConversationMessage
        {
            Id = entity.Id,
            ConversationId = entity.ConversationId,
            Role = new ChatRole(entity.Role),
            ContentJson = entity.ContentJson,
            AuthorName = entity.AuthorName,
            SequenceNumber = entity.SequenceNumber,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        });
    }
}
