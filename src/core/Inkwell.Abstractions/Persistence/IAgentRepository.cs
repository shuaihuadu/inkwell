// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="AgentDefinition"/> 具名 Repository。</summary>
public interface IAgentRepository
{
    /// <summary>新增 Agent。</summary>
    /// <param name="agent">待新增的 Agent。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已新增的 Agent。</returns>
    Task<AgentDefinition> AddAgent(AgentDefinition agent, CancellationToken ct = default);

    /// <summary>更新 Agent。</summary>
    /// <param name="agent">待更新的 Agent。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task UpdateAgent(AgentDefinition agent, CancellationToken ct = default);

    /// <summary>获取指定 Agent。</summary>
    /// <param name="id">Agent 标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Agent 定义。</returns>
    Task<AgentDefinition> GetAgent(Guid id, CancellationToken ct = default);

    /// <summary>幂等：<c>true</c> = 实际删除，<c>false</c> = 本不存在。</summary>
    /// <param name="id">Agent 标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>实际删除时为 <see langword="true"/>；Agent 不存在时为 <see langword="false"/>。</returns>
    Task<bool> DeleteAgent(Guid id, CancellationToken ct = default);

    /// <summary>分页获取 Agent。</summary>
    /// <param name="pagination">分页参数。</param>
    /// <param name="sort">排序条件。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Agent 分页结果。</returns>
    Task<PagedResult<AgentDefinition>> ListAgents(Pagination pagination, SortOrder sort, CancellationToken ct = default);

    /// <summary>查找指定所有者的 Agent。</summary>
    /// <param name="ownerUserId">所有者用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>所有者拥有的 Agent 列表。</returns>
    Task<IReadOnlyList<AgentDefinition>> FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default);

    /// <summary>查找其他用户共享的 Agent。</summary>
    /// <param name="excludingOwnerUserId">需要从结果中排除的所有者用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>共享 Agent 列表。</returns>
    Task<IReadOnlyList<AgentDefinition>> FindSharedAgents(Guid excludingOwnerUserId, CancellationToken ct = default);
}
