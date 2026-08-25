// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class AgentChatMessageUsageSerializer
{
    private static readonly JsonSerializerOptions options = new(JsonSerializerOptions.Web);

    public static string Serialize(UsageDetails usage) => JsonSerializer.Serialize(usage, options);

    public static UsageDetails Deserialize(string usage) =>
        JsonSerializer.Deserialize<UsageDetails>(usage, options)
        ?? throw new JsonException("The persisted token usage JSON deserialized to null.");
}