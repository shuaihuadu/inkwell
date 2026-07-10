// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>Agent 定义管理业务对外接口：CRUD + 共享 + 克隆。</summary>
public interface IAgentService
{
    Task<AgentDefinition> CreateAgentAsync(AgentUpsertRequest request, Guid ownerUserId, CancellationToken ct = default);

    Task<AgentDefinition> UpdateAgentAsync(Guid agentId, AgentUpsertRequest request, Guid actorUserId, CancellationToken ct = default);

    /// <summary>硬删除（HD-002 Q5）；幂等，<c>true</c> = 实际删除，<c>false</c> = 本不存在。</summary>
    Task<bool> DeleteAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default);

    Task<AgentDefinition> GetAgentAsync(Guid agentId, Guid requestingUserId, CancellationToken ct = default);

    Task<IReadOnlyList<AgentSummary>> ListMyAgentsAsync(Guid ownerUserId, CancellationToken ct = default);

    Task<IReadOnlyList<AgentSummary>> ListSharedAgentsAsync(Guid excludingOwnerUserId, CancellationToken ct = default);

    Task ShareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default);

    Task UnshareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>管理员强制撤销共享；<paramref name="actorUserId"/> 是否 IsSuper 由 Inkwell.WebApi 授权中间件前置校验，本方法不重复校验。</summary>
    Task RevokeShareAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default);

    Task<AgentDefinition> CloneAgentAsync(Guid agentId, Guid newOwnerUserId, CancellationToken ct = default);
}
