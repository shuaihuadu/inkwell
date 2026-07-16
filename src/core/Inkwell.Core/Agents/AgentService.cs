// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="IAgentService"/> 唯一实现；CRUD / 共享 / 克隆的完整业务逻辑。</summary>
internal sealed class AgentService(
    IPersistenceProvider persistence,
    IAgentBuildOptionsResolver buildOptionsResolver) : IAgentService
{
    private readonly IAgentRepository _agents = persistence.GetRepository<IAgentRepository>();

    public async Task<AgentDefinition> CreateAgentAsync(AgentUpsertRequest request, Guid ownerUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBasicFields(request);

        AgentBuildOptions buildOptions = await buildOptionsResolver.ResolveAsync(request, ct).ConfigureAwait(false);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentDefinition agent = new()
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = ownerUserId,
            Name = request.Name,
            AvatarUri = request.AvatarUri,
            Description = request.Description,
            Instructions = request.Instructions,
            BuildOptions = buildOptions,
            CreatedTime = now,
            UpdatedTime = now,
        };

        return await this._agents.AddAgent(agent, ct).ConfigureAwait(false);
    }

    public async Task<AgentDefinition> UpdateAgentAsync(Guid agentId, AgentUpsertRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBasicFields(request);

        AgentDefinition agent = await this._agents.GetAgent(agentId, ct).ConfigureAwait(false);
        ValidateOwnership(agent, actorUserId);

        AgentBuildOptions buildOptions = await buildOptionsResolver.ResolveAsync(request, ct).ConfigureAwait(false);
        AgentDefinition updated = agent with
        {
            Name = request.Name,
            AvatarUri = request.AvatarUri,
            Description = request.Description,
            Instructions = request.Instructions,
            BuildOptions = buildOptions,
            UpdatedTime = DateTimeOffset.UtcNow,
        };

        await this._agents.UpdateAgent(updated, ct).ConfigureAwait(false);

        return updated;
    }

    public async Task<bool> DeleteAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
        await persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentDefinition agent = await this._agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

            ValidateOwnership(agent, actorUserId);

            return await this._agents.DeleteAgent(agentId, innerCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public async Task<AgentDefinition> GetAgentAsync(Guid agentId, Guid requestingUserId, CancellationToken ct = default)
    {
        AgentDefinition agent = await this._agents.GetAgent(agentId, ct).ConfigureAwait(false);

        if (agent.OwnerUserId != requestingUserId && !agent.IsShared)
        {
            throw new UnauthorizedAccessException($"User '{requestingUserId}' cannot access agent '{agent.Id}'.");
        }

        return agent;
    }

    public async Task<IReadOnlyList<AgentListItem>> ListMyAgentsAsync(Guid ownerUserId, CancellationToken ct = default)
    {
        IReadOnlyList<AgentDefinition> mine = await this._agents.FindAgentsByOwner(ownerUserId, ct).ConfigureAwait(false);

        return ToAgentListItems(mine);
    }

    public async Task<IReadOnlyList<AgentListItem>> ListSharedAgentsAsync(Guid excludingOwnerUserId, CancellationToken ct = default)
    {
        IReadOnlyList<AgentDefinition> shared = await this._agents.FindSharedAgents(excludingOwnerUserId, ct).ConfigureAwait(false);

        return ToAgentListItems(shared);
    }

    public Task ShareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
        persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentDefinition agent = await this._agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

            ValidateOwnership(agent, actorUserId);

            await this._agents.UpdateAgent(agent with { IsShared = true }, innerCt).ConfigureAwait(false);
        }, ct);

    public Task UnshareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
        persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentDefinition agent = await this._agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

            ValidateOwnership(agent, actorUserId);

            await this._agents.UpdateAgent(agent with { IsShared = false }, innerCt).ConfigureAwait(false);
        }, ct);

    public Task RevokeShareAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
        persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentDefinition agent = await this._agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

            await this._agents.UpdateAgent(agent with { IsShared = false, SharedRevokedByAdminTime = DateTimeOffset.UtcNow }, innerCt).ConfigureAwait(false);
        }, ct);

    public async Task<AgentDefinition> CloneAgentAsync(Guid agentId, Guid newOwnerUserId, CancellationToken ct = default)
    {
        AgentDefinition source = await this._agents.GetAgent(agentId, ct).ConfigureAwait(false);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentDefinition clone = new()
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = newOwnerUserId,
            Name = $"{source.Name}（副本）",
            AvatarUri = source.AvatarUri,
            Description = source.Description,
            Instructions = source.Instructions,
            BuildOptions = source.BuildOptions,
            CreatedTime = now,
            UpdatedTime = now,
        };

        return await this._agents.AddAgent(clone, ct).ConfigureAwait(false);
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

    private static IReadOnlyList<AgentListItem> ToAgentListItems(IReadOnlyList<AgentDefinition> agents) =>
        [.. agents.Select(agent => new AgentListItem(
            agent.Id,
            agent.Name,
            agent.AvatarUri,
            agent.Description is null ? null : agent.Description[..Math.Min(60, agent.Description.Length)],
            agent.OwnerUserId,
            agent.IsShared,
            agent.LatestPublishedVersionNumber,
            agent.UpdatedTime))];
}
