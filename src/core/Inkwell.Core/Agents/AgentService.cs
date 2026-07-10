// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="IAgentService"/> 唯一实现；CRUD / 共享 / 克隆的完整业务逻辑。</summary>
internal sealed class AgentService(IAgentRepository agents, IPersistenceProvider persistence) : IAgentService
{
    public async Task<AgentDefinition> CreateAgentAsync(AgentUpsertRequest request, Guid ownerUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBasicFields(request);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentDefinition agent = new()
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = ownerUserId,
            Name = request.Name,
            AvatarUri = request.AvatarUri,
            Description = request.Description,
            Instructions = request.Instructions,
            ModelId = request.ModelId,
            ModelParameters = request.ModelParameters,
            ToolBindings = request.ToolBindings ?? [],
            SkillBindings = request.SkillBindings ?? [],
            CreatedTime = now,
            UpdatedTime = now,
        };

        return await persistence.ExecuteInTransactionAsync(
            innerCt => agents.AddAgent(agent, innerCt),
            ct).ConfigureAwait(false);
    }

    public async Task<AgentDefinition> UpdateAgentAsync(Guid agentId, AgentUpsertRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBasicFields(request);

        return await persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentDefinition agent = await agents.GetAgent(agentId, innerCt).ConfigureAwait(false);

            ValidateOwnership(agent, actorUserId);

            AgentDefinition updated = agent with
            {
                Name = request.Name,
                AvatarUri = request.AvatarUri,
                Description = request.Description,
                Instructions = request.Instructions,
                ModelId = request.ModelId,
                ModelParameters = request.ModelParameters,
                ToolBindings = request.ToolBindings ?? [],
                SkillBindings = request.SkillBindings ?? [],
                CurrentVersion = agent.CurrentVersion + 1,
                UpdatedTime = DateTimeOffset.UtcNow,
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

    public async Task<AgentDefinition> GetAgentAsync(Guid agentId, Guid requestingUserId, CancellationToken ct = default) =>
        await agents.GetAgent(agentId, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<AgentSummary>> ListMyAgentsAsync(Guid ownerUserId, CancellationToken ct = default)
    {
        IReadOnlyList<AgentDefinition> mine = await agents.FindAgentsByOwner(ownerUserId, ct).ConfigureAwait(false);

        return [.. mine.Select(ToAgentSummary)];
    }

    public async Task<IReadOnlyList<AgentSummary>> ListSharedAgentsAsync(Guid excludingOwnerUserId, CancellationToken ct = default)
    {
        IReadOnlyList<AgentDefinition> shared = await agents.FindSharedAgents(excludingOwnerUserId, ct).ConfigureAwait(false);

        return [.. shared.Select(ToAgentSummary)];
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

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentDefinition clone = source with
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = newOwnerUserId,
            Name = $"{source.Name}（副本）",
            IsShared = false,
            SharedRevokedByAdminTime = null,
            CurrentVersion = 1,
            CreatedTime = now,
            UpdatedTime = now,
        };

        return await persistence.ExecuteInTransactionAsync(
            innerCt => agents.AddAgent(clone, innerCt),
            ct).ConfigureAwait(false);
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

    private static AgentSummary ToAgentSummary(AgentDefinition agent) => new(
        agent.Id,
        agent.Name,
        agent.AvatarUri,
        agent.Description is null ? null : agent.Description[..Math.Min(60, agent.Description.Length)],
        agent.OwnerUserId,
        agent.IsShared,
        agent.CurrentVersion,
        agent.UpdatedTime);
}
