
namespace Inkwell;

/// <summary><see cref="AgentConversation"/> 具名 Repository。</summary>
public interface IAgentConversationRepository
{
    Task<AgentConversation> AddConversation(AgentConversation conversation, CancellationToken ct = default);

    Task<AgentConversation> GetConversation(Guid id, CancellationToken ct = default);

    Task<AgentConversation> UpdateConversation(AgentConversation conversation, CancellationToken ct = default);

    Task<PagedResult<AgentConversation>> ListConversationsByAgent(Guid agentId, Guid ownerUserId, Pagination pagination, SortOrder sort, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> FindUsedAgentIdsByOwner(Guid ownerUserId, CancellationToken ct = default);

    /// <summary>键 = AgentId，值 = 该 (AgentId, viewerUserId) 下最新一条消息的 CreatedTime。</summary>
    Task<IReadOnlyDictionary<Guid, DateTimeOffset>> FindLastActivityByAgents(IReadOnlyList<Guid> agentIds, Guid viewerUserId, CancellationToken ct = default);
}
