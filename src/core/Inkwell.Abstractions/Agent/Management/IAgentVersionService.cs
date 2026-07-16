// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 管理 Agent 发布、历史版本读取与回滚。
/// </summary>
public interface IAgentVersionService
{
    /// <summary>
    /// 获取指定 Agent 当前可调用的发布版本。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="requestingUserId">请求用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前发布且请求用户可见的版本。</returns>
    Task<AgentVersion> GetPublishedVersionAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定的可调用发布版本。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="versionId">版本标识。</param>
    /// <param name="requestingUserId">请求用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>指定且请求用户可见的发布版本。</returns>
    Task<AgentVersion> GetPublishedVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定版本。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="versionId">版本标识。</param>
    /// <param name="requestingUserId">请求用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到且请求用户可见的版本。</returns>
    Task<AgentVersion> GetVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定 Agent 的版本列表。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="requestingUserId">请求用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按版本号降序排列的版本集合。</returns>
    Task<IReadOnlyList<AgentVersion>> ListVersionsAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将当前 Agent 定义发布为新的不可变版本。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="actorUserId">操作用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>发布后的不可变版本。</returns>
    Task<AgentVersion> PublishAsync(Guid agentId, Guid actorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从历史版本快照创建并发布一个新的当前版本。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="sourceVersionId">作为回滚来源的历史版本标识。</param>
    /// <param name="actorUserId">操作用户标识。</param>
    /// <param name="changeSummary">变更摘要。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新生成的已发布版本。</returns>
    Task<AgentVersion> RollbackAsync(Guid agentId, Guid sourceVersionId, Guid actorUserId, string? changeSummary = null, CancellationToken cancellationToken = default);
}