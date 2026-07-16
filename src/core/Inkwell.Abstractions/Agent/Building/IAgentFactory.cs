// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <summary>
/// 根据当前 Agent 定义或不可变发布版本构建 MAF Agent。
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// 根据当前可编辑 Agent 定义构建 MAF Agent。
    /// </summary>
    /// <param name="agent">当前可编辑 Agent 定义。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>构建完成的 MAF Agent。</returns>
    ValueTask<AIAgent> BuildAsync(AgentDefinition agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据不可变发布版本构建 MAF Agent。
    /// </summary>
    /// <param name="version">不可变 Agent 发布版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>构建完成的 MAF Agent。</returns>
    ValueTask<AIAgent> BuildAsync(AgentVersion version, CancellationToken cancellationToken = default);
}
