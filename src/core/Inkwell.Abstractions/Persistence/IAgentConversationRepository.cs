// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="AgentSessionDefinition"/> 具名 Repository。</summary>
public interface IAgentConversationRepository
{
    Task<AgentSessionDefinition> AddConversation(AgentSessionDefinition conversation, CancellationToken ct = default);

    Task<AgentSessionDefinition> GetConversation(Guid id, CancellationToken ct = default);

    Task<AgentSessionDefinition> UpdateConversation(AgentSessionDefinition conversation, CancellationToken ct = default);

    Task<PagedResult<AgentSessionDefinition>> ListConversationsByAgent(Guid agentId, Guid ownerUserId, Pagination pagination, SortOrder sort, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> FindUsedAgentIdsByOwner(Guid ownerUserId, CancellationToken ct = default);

    /// <summary>键 = AgentId，值 = 该 (AgentId, viewerUserId) 下最新一条消息的 CreatedTime。</summary>
    Task<IReadOnlyDictionary<Guid, DateTimeOffset>> FindLastActivityByAgents(IReadOnlyList<Guid> agentIds, Guid viewerUserId, CancellationToken ct = default);
}
