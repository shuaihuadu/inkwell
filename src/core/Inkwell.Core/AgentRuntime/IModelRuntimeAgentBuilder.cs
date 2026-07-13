// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <summary>
/// 定义特定模型运行时的 MAF Agent 构建器。
/// </summary>
internal interface IModelRuntimeAgentBuilder
{
    /// <summary>
    /// 获取运行时连接标识。
    /// </summary>
    string RuntimeId { get; }

    /// <summary>
    /// 使用已解析的模型定义构建 MAF Agent。
    /// </summary>
    /// <param name="model">已解析的模型定义。</param>
    /// <param name="agentVersion">Agent 版本快照。</param>
    /// <param name="agentBuildOptions">Agent 构建选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>MAF Agent。</returns>
    AIAgent Build(
        ModelDefinition model,
        AgentVersion agentVersion,
        AgentBuildOptions agentBuildOptions,
        CancellationToken cancellationToken = default);
}
