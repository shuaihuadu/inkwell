// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Collections.Immutable;

namespace Inkwell;

/// <summary><see cref="IAgentService"/> 唯一实现；CRUD / 共享 / 克隆的完整业务逻辑。</summary>
internal sealed class AgentService(
    IAgentRepository agents,
    IAgentVersionRepository versions,
    IPersistenceProvider persistence) : IAgentService
{
    public async Task<AgentDefinition> CreateAgentAsync(AgentUpsertRequest request, Guid ownerUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBasicFields(request);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentSnapshot snapshot = ToSnapshot(request);
        AgentDefinition agent = new()
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = ownerUserId,
            CreatedTime = now,
            UpdatedTime = now,
        };

        return await persistence.ExecuteInTransactionAsync(async innerCancellationToken =>
        {
            AgentDefinition savedAgent = await agents.AddAgent(agent, innerCancellationToken).ConfigureAwait(false);
            AgentVersion draft = CreateDraft(savedAgent, snapshot, ownerUserId, now);
            AgentVersion savedDraft = await versions.AddVersionAsync(draft, innerCancellationToken).ConfigureAwait(false);
            AgentDefinition updatedAgent = savedAgent with { DraftVersionId = savedDraft.Id };
            await agents.UpdateAgent(updatedAgent, innerCancellationToken).ConfigureAwait(false);

            return updatedAgent;
        }, ct).ConfigureAwait(false);
    }

    public async Task<AgentDefinition> UpdateAgentAsync(Guid agentId, AgentUpsertRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBasicFields(request);

        return await persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentDefinition agent = await agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

            ValidateOwnership(agent, actorUserId);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            int nextVersionNumber = checked(agent.LatestPublishedVersionNumber + 1);
            AgentSnapshot snapshot = ToSnapshot(request);
            AgentVersion published;

            if (agent.DraftVersionId is Guid draftVersionId)
            {
                AgentVersion draft = await versions.GetVersionAsync(draftVersionId, innerCt).ConfigureAwait(false);

                if (draft.AgentId != agentId || draft.Status != AgentVersionStatus.Draft)
                {
                    throw new InvalidOperationException($"Agent draft pointer is invalid: agentId={agentId}, versionId={draftVersionId}");
                }

                published = draft with
                {
                    VersionNumber = nextVersionNumber,
                    Status = AgentVersionStatus.Published,
                    Snapshot = snapshot,
                    CreatedByUserId = actorUserId,
                    UpdatedTime = now,
                    PublishedTime = now,
                };
                published = await versions.UpdateVersionAsync(published, innerCt).ConfigureAwait(false);
            }
            else
            {
                published = new AgentVersion
                {
                    Id = Guid.CreateVersion7(),
                    AgentId = agentId,
                    VersionNumber = nextVersionNumber,
                    Status = AgentVersionStatus.Published,
                    Snapshot = snapshot,
                    CreatedByUserId = actorUserId,
                    CreatedTime = now,
                    UpdatedTime = now,
                    PublishedTime = now,
                };
                published = await versions.AddVersionAsync(published, innerCt).ConfigureAwait(false);
            }

            AgentDefinition updated = agent with
            {
                CurrentPublishedVersionId = published.Id,
                DraftVersionId = null,
                LatestPublishedVersionNumber = nextVersionNumber,
                UpdatedTime = now,
            };

            await agents.UpdateAgent(updated, innerCt).ConfigureAwait(false);

            return updated;
        }, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
        await persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentDefinition agent = await agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

            ValidateOwnership(agent, actorUserId);

            return await agents.DeleteAgent(agentId, innerCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public async Task<AgentDefinition> GetAgentAsync(Guid agentId, Guid requestingUserId, CancellationToken ct = default)
    {
        AgentDefinition agent = await agents.GetAgent(agentId, ct).ConfigureAwait(false);

        if (agent.OwnerUserId != requestingUserId && !agent.IsShared)
        {
            throw new UnauthorizedAccessException($"User '{requestingUserId}' cannot access agent '{agent.Id}'.");
        }

        return agent;
    }

    public async Task<IReadOnlyList<AgentSummary>> ListMyAgentsAsync(Guid ownerUserId, CancellationToken ct = default)
    {
        IReadOnlyList<AgentDefinition> mine = await agents.FindAgentsByOwner(ownerUserId, ct).ConfigureAwait(false);

        return await this.ToAgentSummariesAsync(mine, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AgentSummary>> ListSharedAgentsAsync(Guid excludingOwnerUserId, CancellationToken ct = default)
    {
        IReadOnlyList<AgentDefinition> shared = await agents.FindSharedAgents(excludingOwnerUserId, ct).ConfigureAwait(false);

        return await this.ToAgentSummariesAsync(shared, ct).ConfigureAwait(false);
    }

    public Task ShareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
        persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentDefinition agent = await agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

            ValidateOwnership(agent, actorUserId);

            await agents.UpdateAgent(agent with { IsShared = true }, innerCt).ConfigureAwait(false);
        }, ct);

    public Task UnshareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
        persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentDefinition agent = await agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

            ValidateOwnership(agent, actorUserId);

            await agents.UpdateAgent(agent with { IsShared = false }, innerCt).ConfigureAwait(false);
        }, ct);

    public Task RevokeShareAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
        persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentDefinition agent = await agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

            await agents.UpdateAgent(agent with { IsShared = false, SharedRevokedByAdminTime = DateTimeOffset.UtcNow }, innerCt).ConfigureAwait(false);
        }, ct);

    public async Task<AgentDefinition> CloneAgentAsync(Guid agentId, Guid newOwnerUserId, CancellationToken ct = default)
    {
        AgentDefinition source = await agents.GetAgent(agentId, ct).ConfigureAwait(false);
        Guid sourceVersionId = source.DraftVersionId ?? source.CurrentPublishedVersionId
            ?? throw new InvalidOperationException($"Agent has no version to clone: agentId={agentId}");
        AgentVersion sourceVersion = await versions.GetVersionAsync(sourceVersionId, ct).ConfigureAwait(false);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentDefinition clone = new()
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = newOwnerUserId,
            CreatedTime = now,
            UpdatedTime = now,
        };

        return await persistence.ExecuteInTransactionAsync(async innerCancellationToken =>
        {
            AgentDefinition savedClone = await agents.AddAgent(clone, innerCancellationToken).ConfigureAwait(false);
            AgentSnapshot cloneSnapshot = sourceVersion.Snapshot with { Name = $"{sourceVersion.Snapshot.Name}（副本）" };
            AgentVersion cloneDraft = CreateDraft(savedClone, cloneSnapshot, newOwnerUserId, now);
            AgentVersion savedDraft = await versions.AddVersionAsync(cloneDraft, innerCancellationToken).ConfigureAwait(false);
            AgentDefinition updatedClone = savedClone with { DraftVersionId = savedDraft.Id };
            await agents.UpdateAgent(updatedClone, innerCancellationToken).ConfigureAwait(false);

            return updatedClone;
        }, ct).ConfigureAwait(false);
    }

    private static void ValidateOwnership(AgentDefinition agent, Guid actorUserId)
    {
        if (agent.OwnerUserId != actorUserId)
        {
            throw new UnauthorizedAccessException($"User '{actorUserId}' is not the owner of agent '{agent.Id}'.");
        }
    }

    private static void ValidateBasicFields(AgentUpsertRequest request)
    {
        if (string.IsNullOrEmpty(request.Name) || request.Name.Length is < 1 or > 50)
        {
            throw new ArgumentException("Name must be between 1 and 50 characters.", nameof(request));
        }

        if (request.Description is { Length: > 500 })
        {
            throw new ArgumentException("Description must not exceed 500 characters.", nameof(request));
        }
    }

    private async Task<IReadOnlyList<AgentSummary>> ToAgentSummariesAsync(IReadOnlyList<AgentDefinition> agentDefinitions, CancellationToken cancellationToken)
    {
        List<Guid> activeVersionIds = [.. agentDefinitions
            .Select(agent => agent.DraftVersionId ?? agent.CurrentPublishedVersionId)
            .OfType<Guid>()];
        IReadOnlyDictionary<Guid, AgentVersion> activeVersions = await versions.FindVersionsByIdsAsync(activeVersionIds, cancellationToken).ConfigureAwait(false);

        List<AgentSummary> summaries = [];

        foreach (AgentDefinition agent in agentDefinitions)
        {
            Guid? activeVersionId = agent.DraftVersionId ?? agent.CurrentPublishedVersionId;

            if (activeVersionId is not Guid versionId || !activeVersions.TryGetValue(versionId, out AgentVersion? version))
            {
                continue;
            }

            AgentSnapshot snapshot = version.Snapshot;
            summaries.Add(new AgentSummary(
                agent.Id,
                snapshot.Name,
                snapshot.AvatarUri,
                snapshot.Description is null ? null : snapshot.Description[..Math.Min(60, snapshot.Description.Length)],
                agent.OwnerUserId,
                agent.IsShared,
                agent.LatestPublishedVersionNumber,
                agent.UpdatedTime));
        }

        return summaries;
    }

    private static AgentSnapshot ToSnapshot(AgentUpsertRequest request) => new()
    {
        Name = request.Name,
        AvatarUri = request.AvatarUri,
        Description = request.Description,
        Instructions = request.Instructions,
        ModelId = request.ModelId,
        ModelParameters = request.ModelParameters,
        ToolBindings = request.ToolBindings?.ToImmutableArray() ?? [],
        SkillBindings = request.SkillBindings?.ToImmutableArray() ?? [],
    };

    private static AgentVersion CreateDraft(AgentDefinition agent, AgentSnapshot snapshot, Guid actorUserId, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        AgentId = agent.Id,
        VersionNumber = checked(agent.LatestPublishedVersionNumber + 1),
        Status = AgentVersionStatus.Draft,
        Snapshot = snapshot,
        CreatedByUserId = actorUserId,
        CreatedTime = now,
        UpdatedTime = now,
    };
}
