// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentChatMessageSerializer
{
    private static readonly JsonSerializerOptions options = new()
    {
        AllowOutOfOrderMetadataProperties = true,
    };

    public static string Serialize(ChatMessage message) => JsonSerializer.Serialize(message, options);

    public static ChatMessage Deserialize(string message) =>
        JsonSerializer.Deserialize<ChatMessage>(message, options)
        ?? throw new JsonException("The persisted chat message JSON deserialized to null.");
}