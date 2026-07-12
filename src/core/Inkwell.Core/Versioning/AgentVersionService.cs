// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// <see cref="IAgentVersionService"/> 默认实现。
/// </summary>
internal sealed class AgentVersionService(
    IAgentRepository agents,
    IAgentVersionRepository versions,
    IPersistenceProvider persistence) : IAgentVersionService
{
    public async Task<AgentVersion> GetVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        AgentDefinition agent = await agents.GetAgent(agentId, cancellationToken).ConfigureAwait(false);
        ValidateVisibility(agent, requestingUserId);

        AgentVersion version = await versions.GetVersionAsync(versionId, cancellationToken).ConfigureAwait(false);
        ValidateVersionBelongsToAgent(version, agentId);

        return version;
    }

    public async Task<IReadOnlyList<AgentVersion>> ListVersionsAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        AgentDefinition agent = await agents.GetAgent(agentId, cancellationToken).ConfigureAwait(false);
        ValidateVisibility(agent, requestingUserId);

        IReadOnlyList<AgentVersion> agentVersions = await versions.ListVersionsByAgentAsync(agentId, cancellationToken).ConfigureAwait(false);

        return agent.OwnerUserId == requestingUserId
            ? agentVersions
            : [.. agentVersions.Where(version => version.Status == AgentVersionStatus.Published)];
    }

    public Task<AgentVersion> SaveDraftAsync(
        Guid agentId,
        AgentSnapshot snapshot,
        Guid actorUserId,
        string? changeSummary = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateSnapshot(snapshot);

        return persistence.ExecuteInTransactionAsync(async innerCancellationToken =>
        {
            AgentDefinition agent = await agents.GetAgent(agentId, innerCancellationToken).ConfigureAwait(false);
            ValidateOwnership(agent, actorUserId);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (agent.DraftVersionId is Guid draftVersionId)
            {
                AgentVersion draft = await versions.GetVersionAsync(draftVersionId, innerCancellationToken).ConfigureAwait(false);
                ValidateDraft(draft, agentId);

                AgentVersion updatedDraft = draft with
                {
                    Snapshot = snapshot,
                    ChangeSummary = changeSummary,
                    UpdatedTime = now,
                };

                return await versions.UpdateVersionAsync(updatedDraft, innerCancellationToken).ConfigureAwait(false);
            }

            AgentVersion newDraft = CreateDraft(agent, snapshot, actorUserId, changeSummary, now);
            AgentVersion savedDraft = await versions.AddVersionAsync(newDraft, innerCancellationToken).ConfigureAwait(false);
            await agents.UpdateAgent(agent with { DraftVersionId = savedDraft.Id, UpdatedTime = now }, innerCancellationToken).ConfigureAwait(false);

            return savedDraft;
        }, cancellationToken);
    }

    public Task<AgentVersion> PublishDraftAsync(Guid agentId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        persistence.ExecuteInTransactionAsync(async innerCancellationToken =>
        {
            AgentDefinition agent = await agents.GetAgent(agentId, innerCancellationToken).ConfigureAwait(false);
            ValidateOwnership(agent, actorUserId);

            Guid draftVersionId = agent.DraftVersionId
                ?? throw new InvalidOperationException($"Agent has no draft version: agentId={agentId}");
            AgentVersion draft = await versions.GetVersionAsync(draftVersionId, innerCancellationToken).ConfigureAwait(false);
            ValidateDraft(draft, agentId);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            int nextVersionNumber = checked(agent.LatestPublishedVersionNumber + 1);
            AgentVersion published = draft with
            {
                VersionNumber = nextVersionNumber,
                Status = AgentVersionStatus.Published,
                UpdatedTime = now,
                PublishedTime = now,
            };

            AgentVersion savedVersion = await versions.UpdateVersionAsync(published, innerCancellationToken).ConfigureAwait(false);
            AgentDefinition updatedAgent = agent with
            {
                CurrentPublishedVersionId = savedVersion.Id,
                DraftVersionId = null,
                LatestPublishedVersionNumber = nextVersionNumber,
                UpdatedTime = now,
            };
            await agents.UpdateAgent(updatedAgent, innerCancellationToken).ConfigureAwait(false);

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
            AgentDefinition agent = await agents.GetAgent(agentId, innerCancellationToken).ConfigureAwait(false);
            ValidateOwnership(agent, actorUserId);

            if (agent.DraftVersionId is not null)
            {
                throw new InvalidOperationException($"Agent has unpublished draft changes: agentId={agentId}");
            }

            AgentVersion source = await versions.GetVersionAsync(sourceVersionId, innerCancellationToken).ConfigureAwait(false);
            ValidateVersionBelongsToAgent(source, agentId);

            if (source.Status != AgentVersionStatus.Published)
            {
                throw new InvalidOperationException($"Rollback source must be published: versionId={sourceVersionId}");
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            int nextVersionNumber = checked(agent.LatestPublishedVersionNumber + 1);
            AgentVersion rollbackVersion = new()
            {
                Id = Guid.CreateVersion7(),
                AgentId = agentId,
                VersionNumber = nextVersionNumber,
                Status = AgentVersionStatus.Published,
                Snapshot = source.Snapshot,
                CreatedByUserId = actorUserId,
                ChangeSummary = changeSummary ?? $"Rollback from v{source.VersionNumber}",
                CreatedTime = now,
                UpdatedTime = now,
                PublishedTime = now,
            };

            AgentVersion savedVersion = await versions.AddVersionAsync(rollbackVersion, innerCancellationToken).ConfigureAwait(false);
            AgentDefinition updatedAgent = agent with
            {
                CurrentPublishedVersionId = savedVersion.Id,
                LatestPublishedVersionNumber = nextVersionNumber,
                UpdatedTime = now,
            };
            await agents.UpdateAgent(updatedAgent, innerCancellationToken).ConfigureAwait(false);

            return savedVersion;
        }, cancellationToken);

    private static AgentVersion CreateDraft(
        AgentDefinition agent,
        AgentSnapshot snapshot,
        Guid actorUserId,
        string? changeSummary,
        DateTimeOffset now) => new()
        {
            Id = Guid.CreateVersion7(),
            AgentId = agent.Id,
            VersionNumber = checked(agent.LatestPublishedVersionNumber + 1),
            Status = AgentVersionStatus.Draft,
            Snapshot = snapshot,
            CreatedByUserId = actorUserId,
            ChangeSummary = changeSummary,
            CreatedTime = now,
            UpdatedTime = now,
        };

    private static void ValidateDraft(AgentVersion version, Guid agentId)
    {
        ValidateVersionBelongsToAgent(version, agentId);

        if (version.Status != AgentVersionStatus.Draft || version.PublishedTime is not null)
        {
            throw new InvalidOperationException($"Agent version is not an editable draft: versionId={version.Id}");
        }
    }

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

    private static void ValidateSnapshot(AgentSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.Name) || snapshot.Name.Length > 50)
        {
            throw new ArgumentException("Agent snapshot name must be between 1 and 50 characters.", nameof(snapshot));
        }

        if (snapshot.Description is { Length: > 500 })
        {
            throw new ArgumentException("Agent snapshot description must not exceed 500 characters.", nameof(snapshot));
        }
    }
}