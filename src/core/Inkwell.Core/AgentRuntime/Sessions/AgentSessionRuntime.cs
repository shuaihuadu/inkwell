// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <summary>
/// 协调 Inkwell 业务 Session 与对应版本构建出的 MAF <see cref="AgentSession"/>。
/// </summary>
internal static class AgentSessionRuntime
{
    /// <summary>
    /// 使用对应 Agent 创建新 MAF Session，或从业务 Session 保存的状态恢复。
    /// </summary>
    /// <param name="agent">由业务 Session 的 AgentVersionId 对应版本构建出的 Agent。</param>
    /// <param name="sessionDefinition">Inkwell 业务 Session。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>与指定 Agent 兼容且已附加业务 Session 标识的 MAF Session。</returns>
    internal static async ValueTask<AgentSession> OpenAsync(
        AIAgent agent,
        AgentSessionDefinition sessionDefinition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(sessionDefinition);

        AgentSession session = sessionDefinition.SessionState is JsonElement serializedState
            ? await agent.DeserializeSessionAsync(serializedState, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);

        InkwellChatHistoryProvider.AttachSession(session, sessionDefinition.Id);

        return session;
    }

    /// <summary>
    /// 使用创建该 MAF Session 的同一 Agent 捕获可持久化状态。
    /// </summary>
    /// <param name="agent">创建或恢复 Session 的 Agent。</param>
    /// <param name="session">待序列化的 MAF Session。</param>
    /// <param name="sessionDefinition">对应 Inkwell 业务 Session。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>包含最新 MAF Session 状态的业务 Session。</returns>
    internal static async ValueTask<AgentSessionDefinition> CaptureAsync(
        AIAgent agent,
        AgentSession session,
        AgentSessionDefinition sessionDefinition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(sessionDefinition);

        JsonElement serializedState = await agent.SerializeSessionAsync(session, cancellationToken: cancellationToken).ConfigureAwait(false);

        return sessionDefinition with
        {
            SessionState = serializedState,
            UpdatedTime = DateTimeOffset.UtcNow,
        };
    }
}