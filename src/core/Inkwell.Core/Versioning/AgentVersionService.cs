// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// <see cref="IAgentVersionService"/> 默认实现。
/// </summary>
internal sealed class AgentVersionService(
    IPersistenceProvider persistence) : IAgentVersionService
{
    private readonly IAgentRepository _agents = persistence.GetRepository<IAgentRepository>();
    private readonly IAgentVersionRepository _versions = persistence.GetRepository<IAgentVersionRepository>();

    public async Task<AgentVersion> GetPublishedVersionAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        AgentDefinition agent = await this._agents.GetAgent(agentId, cancellationToken).ConfigureAwait(false);
        ValidateVisibility(agent, requestingUserId);

        Guid publishedVersionId = agent.CurrentPublishedVersionId
            ?? throw new InvalidOperationException($"Agent has no published version: agentId={agentId}");
        AgentVersion version = await this._versions.GetVersionAsync(publishedVersionId, cancellationToken).ConfigureAwait(false);
        ValidateVersionBelongsToAgent(version, agentId);

        return version;
    }

    public Task<AgentVersion> GetPublishedVersionAsync(
        Guid agentId,
        Guid versionId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default) =>
        this.GetVersionAsync(agentId, versionId, requestingUserId, cancellationToken);

    public async Task<AgentVersion> GetVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        AgentDefinition agent = await this._agents.GetAgent(agentId, cancellationToken).ConfigureAwait(false);
        ValidateVisibility(agent, requestingUserId);

        AgentVersion version = await this._versions.GetVersionAsync(versionId, cancellationToken).ConfigureAwait(false);
        ValidateVersionBelongsToAgent(version, agentId);

        return version;
    }

    public async Task<IReadOnlyList<AgentVersion>> ListVersionsAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        AgentDefinition agent = await this._agents.GetAgent(agentId, cancellationToken).ConfigureAwait(false);
        ValidateVisibility(agent, requestingUserId);

        return await this._versions.ListVersionsByAgentAsync(agentId, cancellationToken).ConfigureAwait(false);
    }

    public Task<AgentVersion> PublishAsync(Guid agentId, Guid actorUserId, string? changeSummary = null, CancellationToken cancellationToken = default) =>
        persistence.ExecuteInTransactionAsync(async innerCancellationToken =>
        {
            AgentDefinition agent = await this._agents.GetAgent(agentId, innerCancellationToken).ConfigureAwait(false);
            ValidateOwnership(agent, actorUserId);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            Guid versionId = Guid.CreateVersion7();
            int nextVersionNumber = checked(agent.LatestPublishedVersionNumber + 1);
            AgentSnapshot snapshot = CreateSnapshot(agent);
            AgentVersion version = new()
            {
                Id = versionId,
                AgentId = agentId,
                VersionNumber = nextVersionNumber,
                Snapshot = snapshot,
                CreatedByUserId = actorUserId,
                ChangeSummary = changeSummary,
                CreatedTime = now,
                UpdatedTime = now,
                PublishedTime = now,
            };

            AgentVersion savedVersion = await this._versions.AddVersionAsync(version, innerCancellationToken).ConfigureAwait(false);
            AgentDefinition updatedAgent = agent with
            {
                CurrentPublishedVersionId = savedVersion.Id,
                LatestPublishedVersionNumber = nextVersionNumber,
                UpdatedTime = now,
            };
            await this._agents.UpdateAgent(updatedAgent, innerCancellationToken).ConfigureAwait(false);

            return savedVersion;
        }, cancellationToken);

    public Task<AgentVersion> RollbackAsync(
        Guid agentId,
        Guid sourceVersionId,
        Guid actorUserId,
        string? changeSummary = null,
        CancellationToken cancellationToken = default) =>
        persistence.ExecuteInTransactionAsync(async innerCancellationToken =>
        {
            AgentDefinition agent = await this._agents.GetAgent(agentId, innerCancellationToken).ConfigureAwait(false);
            ValidateOwnership(agent, actorUserId);

            AgentVersion source = await this._versions.GetVersionAsync(sourceVersionId, innerCancellationToken).ConfigureAwait(false);
            ValidateVersionBelongsToAgent(source, agentId);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            Guid versionId = Guid.CreateVersion7();
            int nextVersionNumber = checked(agent.LatestPublishedVersionNumber + 1);
            AgentSnapshot snapshot = source.Snapshot;
            AgentVersion rollbackVersion = new()
            {
                Id = versionId,
                AgentId = agentId,
                VersionNumber = nextVersionNumber,
                Snapshot = snapshot,
                CreatedByUserId = actorUserId,
                ChangeSummary = changeSummary ?? $"Rollback from v{source.VersionNumber}",
                CreatedTime = now,
                UpdatedTime = now,
                PublishedTime = now,
            };

            AgentVersion savedVersion = await this._versions.AddVersionAsync(rollbackVersion, innerCancellationToken).ConfigureAwait(false);
            AgentDefinition updatedAgent = agent with
            {
                Name = snapshot.Name,
                AvatarUri = snapshot.AvatarUri,
                Description = snapshot.Description,
                Instructions = snapshot.Instructions,
                BuildOptions = snapshot.BuildOptions,
                CurrentPublishedVersionId = savedVersion.Id,
                LatestPublishedVersionNumber = nextVersionNumber,
                UpdatedTime = now,
            };
            await this._agents.UpdateAgent(updatedAgent, innerCancellationToken).ConfigureAwait(false);

            return savedVersion;
        }, cancellationToken);

    private static AgentSnapshot CreateSnapshot(AgentDefinition agent) => new()
    {
        Name = agent.Name,
        AvatarUri = agent.AvatarUri,
        Description = agent.Description,
        Instructions = agent.Instructions,
        BuildOptions = agent.BuildOptions,
    };

    private static void ValidateOwnership(AgentDefinition agent, Guid actorUserId)
    {
        if (agent.OwnerUserId != actorUserId)
        {
            throw new UnauthorizedAccessException($"User '{actorUserId}' is not the owner of agent '{agent.Id}'.");
        }
    }

    private static void ValidateVisibility(AgentDefinition agent, Guid requestingUserId)
    {
        if (agent.OwnerUserId != requestingUserId && !agent.IsShared)
        {
            throw new UnauthorizedAccessException($"User '{requestingUserId}' cannot access agent '{agent.Id}'.");
        }
    }

    private static void ValidateVersionBelongsToAgent(AgentVersion version, Guid agentId)
    {
        if (version.AgentId != agentId)
        {
            throw new KeyNotFoundException($"Agent version not found: agentId={agentId}, versionId={version.Id}");
        }
    }
}
