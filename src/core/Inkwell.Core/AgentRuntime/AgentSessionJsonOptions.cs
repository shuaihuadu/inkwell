// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

internal static class AgentSessionJsonOptions
{
    internal static JsonSerializerOptions Default { get; } = new(AgentAbstractionsJsonUtilities.DefaultOptions)
    {
        AllowOutOfOrderMetadataProperties = true,
    };
}