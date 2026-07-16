// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <inheritdoc />
internal sealed class AgentBuildService(
    IAgentVersionService versionService,
    IAgentFactory agentFactory) : IAgentBuildService
{
    /// <inheritdoc />
    public async ValueTask<AIAgent> BuildPublishedAsync(
        Guid agentId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        AgentVersion version = await versionService
            .GetPublishedVersionAsync(agentId, requestingUserId, cancellationToken)
            .ConfigureAwait(false);

        return await agentFactory
            .BuildAsync(version, cancellationToken)
            .ConfigureAwait(false);
    }
}