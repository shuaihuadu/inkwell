// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Microsoft.Extensions.AI;

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentConversationMessageMappingExtensions
{
    public static AgentChatMessage ToModel(this AgentConversationMessageEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AgentChatMessage
        {
            Id = entity.Id,
            SessionId = entity.ConversationId,
            Role = new ChatRole(entity.Role),
            ContentJson = entity.ContentJson,
            AuthorName = entity.AuthorName,
            SequenceNumber = entity.SequenceNumber,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static AgentConversationMessageEntity ToEntity(this AgentChatMessage model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new AgentConversationMessageEntity
        {
            Id = model.Id,
            ConversationId = model.SessionId,
            Role = model.Role.Value,
            ContentJson = model.ContentJson,
            AuthorName = model.AuthorName,
            SequenceNumber = model.SequenceNumber,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }

    public static IQueryable<AgentChatMessage> SelectAsModel(this IQueryable<AgentConversationMessageEntity> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(entity => new AgentChatMessage
        {
            Id = entity.Id,
            SessionId = entity.ConversationId,
            Role = new ChatRole(entity.Role),
            ContentJson = entity.ContentJson,
            AuthorName = entity.AuthorName,
            SequenceNumber = entity.SequenceNumber,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        });
    }
}
