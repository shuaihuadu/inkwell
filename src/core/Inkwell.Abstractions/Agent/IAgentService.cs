// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>Agent 定义管理业务对外接口：CRUD + 共享 + 克隆。</summary>
public interface IAgentService
{
    /// <summary>
    /// 创建 Agent。
    /// </summary>
    /// <param name="request">Agent 创建请求。</param>
    /// <param name="ownerUserId">所有者用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>创建的 Agent 定义。</returns>
    Task<AgentDefinition> CreateAgentAsync(AgentUpsertRequest request, Guid ownerUserId, CancellationToken ct = default);

    /// <summary>
    /// 更新 Agent。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="request">Agent 更新请求。</param>
    /// <param name="actorUserId">执行操作的用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>更新后的 Agent 定义。</returns>
    Task<AgentDefinition> UpdateAgentAsync(Guid agentId, AgentUpsertRequest request, Guid actorUserId, CancellationToken ct = default);

    /// <summary>硬删除（HD-002 Q5）；幂等，<c>true</c> = 实际删除，<c>false</c> = 本不存在。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="actorUserId">执行操作的用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>实际删除时为 <see langword="true"/>；Agent 不存在时为 <see langword="false"/>。</returns>
    Task<bool> DeleteAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>
    /// 获取指定 Agent。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="requestingUserId">请求用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Agent 定义。</returns>
    Task<AgentDefinition> GetAgentAsync(Guid agentId, Guid requestingUserId, CancellationToken ct = default);

    /// <summary>
    /// 获取指定用户拥有的 Agent 列表。
    /// </summary>
    /// <param name="ownerUserId">所有者用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Agent 摘要列表。</returns>
    Task<IReadOnlyList<AgentSummary>> ListMyAgentsAsync(Guid ownerUserId, CancellationToken ct = default);

    /// <summary>
    /// 获取其他用户共享的 Agent 列表。
    /// </summary>
    /// <param name="excludingOwnerUserId">需要从结果中排除的所有者用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>共享 Agent 摘要列表。</returns>
    Task<IReadOnlyList<AgentSummary>> ListSharedAgentsAsync(Guid excludingOwnerUserId, CancellationToken ct = default);

    /// <summary>
    /// 共享指定 Agent。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="actorUserId">执行操作的用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ShareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>
    /// 取消共享指定 Agent。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="actorUserId">执行操作的用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task UnshareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>管理员强制撤销共享；<paramref name="actorUserId"/> 是否 IsSuper 由 Inkwell.WebApi 授权中间件前置校验，本方法不重复校验。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="actorUserId">执行操作的管理员用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task RevokeShareAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>
    /// 将指定 Agent 克隆给新的所有者。
    /// </summary>
    /// <param name="agentId">源 Agent 标识。</param>
    /// <param name="newOwnerUserId">新所有者用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>克隆后的 Agent 定义。</returns>
    Task<AgentDefinition> CloneAgentAsync(Guid agentId, Guid newOwnerUserId, CancellationToken ct = default);
}
