// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <inheritdoc />
internal sealed class AgentBuildService(
    IAgentVersionService versionService,
    IAgentService agentService,
    IAgentFactory agentFactory) : IAgentBuildService
{
    /// <inheritdoc />
    public async ValueTask<AIAgent> BuildDraftAsync(
        Guid agentId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        AgentDefinition agent = await this.GetOwnedDraftAsync(agentId, requestingUserId, cancellationToken).ConfigureAwait(false);

        return await agentFactory
            .BuildAsync(agent, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<AIAgent> BuildDraftTrialAsync(
        Guid agentId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        AgentDefinition agent = await this.GetOwnedDraftAsync(agentId, requestingUserId, cancellationToken).ConfigureAwait(false);

        return await agentFactory
            .BuildAsync(WithoutChatHistory(agent), cancellationToken)
            .ConfigureAwait(false);
    }

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

    /// <inheritdoc />
    public async ValueTask<AIAgent> BuildPublishedConversationAsync(
        Guid agentId,
        Guid versionId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        AgentVersion version = await versionService
            .GetPublishedVersionAsync(agentId, versionId, requestingUserId, cancellationToken)
            .ConfigureAwait(false);

        return await agentFactory
            .BuildConversationAsync(version, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<AIAgent> BuildPublishedTrialAsync(
        Guid agentId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        AgentVersion version = await versionService
            .GetPublishedVersionAsync(agentId, requestingUserId, cancellationToken)
            .ConfigureAwait(false);

        return await agentFactory
            .BuildAsync(WithoutChatHistory(version), cancellationToken)
            .ConfigureAwait(false);
    }

    private static AgentDefinition WithoutChatHistory(AgentDefinition agent) => agent with
    {
        BuildOptions = agent.BuildOptions with { ChatHistoryOptions = null },
    };

    private static AgentVersion WithoutChatHistory(AgentVersion version) => version with
    {
        Snapshot = version.Snapshot with
        {
            BuildOptions = version.Snapshot.BuildOptions with { ChatHistoryOptions = null },
        },
    };

    private async ValueTask<AgentDefinition> GetOwnedDraftAsync(
        Guid agentId,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        AgentDefinition agent = await agentService
            .GetAgentAsync(agentId, requestingUserId, cancellationToken)
            .ConfigureAwait(false);

        if (agent.OwnerUserId != requestingUserId)
        {
            throw new UnauthorizedAccessException($"User '{requestingUserId}' cannot run the draft of agent '{agentId}'.");
        }

        return agent;
    }
}