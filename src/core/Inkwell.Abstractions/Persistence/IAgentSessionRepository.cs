// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="AgentSessionDefinition"/> 具名 Repository。</summary>
public interface IAgentSessionRepository
{
    /// <summary>新增 Agent 会话。</summary>
    /// <param name="sessionDefinition">待新增的会话。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已新增的会话。</returns>
    Task<AgentSessionDefinition> AddSession(AgentSessionDefinition sessionDefinition, CancellationToken ct = default);

    /// <summary>获取指定 Agent 会话。</summary>
    /// <param name="id">会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Agent 会话。</returns>
    Task<AgentSessionDefinition> GetSession(Guid id, CancellationToken ct = default);

    /// <summary>更新 Agent 会话。</summary>
    /// <param name="sessionDefinition">待更新的会话。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>更新后的会话。</returns>
    Task<AgentSessionDefinition> UpdateSession(AgentSessionDefinition sessionDefinition, CancellationToken ct = default);

    /// <summary>分页获取指定 Agent 和所有者的会话。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="ownerUserId">所有者用户标识。</param>
    /// <param name="pagination">分页参数。</param>
    /// <param name="sort">排序条件。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Agent 会话分页结果。</returns>
    Task<PagedResult<AgentSessionDefinition>> ListSessionsByAgent(Guid agentId, Guid ownerUserId, Pagination pagination, SortOrder sort, CancellationToken ct = default);

    /// <summary>查找指定所有者已使用的 Agent 标识。</summary>
    /// <param name="ownerUserId">所有者用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已使用的 Agent 标识列表。</returns>
    Task<IReadOnlyList<Guid>> FindUsedAgentIdsByOwner(Guid ownerUserId, CancellationToken ct = default);

    /// <summary>键 = AgentId，值 = 该 (AgentId, viewerUserId) 下最新一条消息的 CreatedTime。</summary>
    /// <param name="agentIds">Agent 标识列表。</param>
    /// <param name="viewerUserId">查看者用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Agent 标识与最近活动时间的映射。</returns>
    Task<IReadOnlyDictionary<Guid, DateTimeOffset>> FindLastActivityByAgents(IReadOnlyList<Guid> agentIds, Guid viewerUserId, CancellationToken ct = default);
}
