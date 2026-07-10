
namespace Inkwell;

/// <summary><see cref="AgentDefinition"/> 具名 Repository。</summary>
public interface IAgentRepository
{
    Task<AgentDefinition> AddAgent(AgentDefinition agent, CancellationToken ct = default);

    Task UpdateAgent(AgentDefinition agent, CancellationToken ct = default);

    Task<AgentDefinition> GetAgent(Guid id, CancellationToken ct = default);

    /// <summary>幂等：<c>true</c> = 实际删除，<c>false</c> = 本不存在。</summary>
    Task<bool> DeleteAgent(Guid id, CancellationToken ct = default);

    Task<PagedResult<AgentDefinition>> ListAgents(Pagination pagination, SortOrder sort, CancellationToken ct = default);

    Task<IReadOnlyList<AgentDefinition>> FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default);

    Task<IReadOnlyList<AgentDefinition>> FindSharedAgents(Guid excludingOwnerUserId, CancellationToken ct = default);
}
