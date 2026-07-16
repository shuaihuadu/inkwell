// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <summary>
/// 根据 Agent 标识和调用者身份构建可运行的已发布 Agent。
/// </summary>
public interface IAgentBuildService
{
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
}