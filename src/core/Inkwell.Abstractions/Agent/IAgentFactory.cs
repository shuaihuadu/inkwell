// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <summary>
/// 根据不可变 Agent 版本构建可由 MAF Hosting、Workflow 与应用代码直接消费的 Agent。
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// 构建指定版本的 MAF Agent。
    /// </summary>
    /// <param name="agentVersion">包含完整运行时快照的 Agent 版本。</param>
    /// <param name="agentBuildOptions">本次构建需要附加的可执行工具与聊天历史 Provider。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>构建完成的 MAF Agent。</returns>
    ValueTask<AIAgent> BuildAsync(AgentVersion agentVersion, AgentBuildOptions agentBuildOptions, CancellationToken cancellationToken = default);
}
