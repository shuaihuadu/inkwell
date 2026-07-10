// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

public interface IAgentFactory
{
    ValueTask<AIAgent> BulidAsync(AgentVersion agentVersion, AgentBuildOptions agentBuildOptions, CancellationToken cancellationToken = default);
}
