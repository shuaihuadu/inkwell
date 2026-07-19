// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <summary>
/// 根据 Agent 标识和调用者身份构建可运行的已发布 Agent。
/// </summary>
public interface IAgentBuildService
{
    /// <summary>
    /// 构建所有者当前保存的 Agent 草稿。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="requestingUserId">请求用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>构建完成的 MAF Agent。</returns>
    ValueTask<AIAgent> BuildDraftAsync(
        Guid agentId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 构建所有者当前保存的 Agent 草稿用于不持久化聊天历史的临时试运行。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="requestingUserId">请求用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>构建完成的 MAF Agent。</returns>
    ValueTask<AIAgent> BuildDraftTrialAsync(
        Guid agentId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 构建调用者有权访问的当前已发布 Agent。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="requestingUserId">请求用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>构建完成的 MAF Agent。</returns>
    ValueTask<AIAgent> BuildPublishedAsync(
        Guid agentId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 构建调用者有权访问的当前已发布 Agent，用于不持久化聊天历史的临时试运行。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="requestingUserId">请求用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>构建完成的 MAF Agent。</returns>
    ValueTask<AIAgent> BuildPublishedTrialAsync(
        Guid agentId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);
}