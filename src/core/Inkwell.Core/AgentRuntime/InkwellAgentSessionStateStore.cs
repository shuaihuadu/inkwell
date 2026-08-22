// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Inkwell;

/// <summary>
/// 基于 <see cref="IAgentSessionStateRepository"/> 的 Agent Session 检查点存储，
/// 把 MAF <see cref="AgentSession"/> 序列化后持久化到 Inkwell 的 <c>AgentSessionStates</c> 表。
/// </summary>
/// <remarks>
/// <para>
/// 检查点以产品会话标识 <see cref="AgentConversation.Id"/> 定位。本类型不实现 MAF
/// <c>AgentSessionStore</c>：该基类以 opaque 字符串 <c>sessionStoreId</c> 作为查找键，
/// 而 MAF AG-UI / A2A Hosting 只会通过 <c>GetKeyedService</c> 解析 Session Store 并强制包裹
/// <c>IsolationKeyScopedAgentSessionStore</c>（把 id 改写为 <c>"{isolationKey}::{sessionStoreId}"</c>），
/// 改写后的 id 无法映射回产品会话。Inkwell 的协议层会话延续由
/// <see cref="IAgentConversationService"/> 承接，本类型只服务于该编排。
/// </para>
/// <para>
/// 状态行的生命周期完全由本类型拥有：首次 <see cref="SaveSessionAsync"/> 时建行，
/// <see cref="DeleteSessionStateAsync"/> 时删行，调用方不需要（也不应该）预先创建空行。
/// </para>
/// <para>
/// 本类型不做任何调用方身份校验，越权校验必须由调用方在进入本类型之前完成；
/// <see cref="AgentConversationService"/> 通过 <c>GetAuthorizedConversationAsync</c> 承担该职责。
/// </para>
/// </remarks>
internal sealed class InkwellAgentSessionStateStore(
    IPersistenceProvider persistence,
    TimeProvider timeProvider,
    ILogger<InkwellAgentSessionStateStore> logger)
{
    private const string PendingApprovalRequestsStateKey = "_pendingApprovalRequests";

    private readonly IAgentSessionStateRepository _sessionStates = persistence.GetRepository<IAgentSessionStateRepository>();

    /// <summary>
    /// 读取指定会话的 Session 检查点并恢复为 MAF Session。
    /// </summary>
    /// <param name="agent">承载本次运行的 Agent。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>恢复出的 Session；无可用检查点时为新建的空 Session。</returns>
    /// <remarks>状态行不存在、内容为空或反序列化失败时，一律回退为新建空 Session，不向调用方抛出。</remarks>
    public async Task<AgentSession> GetSessionAsync(
        AIAgent agent,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        AgentSessionState? state = await this._sessionStates
            .FindSessionStateByConversation(conversationId, cancellationToken)
            .ConfigureAwait(false);

        if (state is null
            || string.IsNullOrWhiteSpace(state.SessionState)
            || string.Equals(state.SessionState, AgentSessionState.Empty, StringComparison.Ordinal))
        {
            return await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(state.SessionState);
            AgentSession session = await agent
                .DeserializeSessionAsync(document.RootElement, AgentSessionJsonOptions.Default, cancellationToken)
                .ConfigureAwait(false);

            if (session.StateBag.TryGetValue<List<ToolApprovalRequestContent>>(
                PendingApprovalRequestsStateKey,
                out List<ToolApprovalRequestContent>? pendingApprovals,
                AgentSessionJsonOptions.Default)
                && pendingApprovals is not null)
            {
                List<ToolApprovalRequestContent> supportedApprovals =
                [.. pendingApprovals.Where(static approval => approval.ToolCall is not FunctionCallContent
                {
                    Name: AgentSkillsProvider.LoadSkillToolName
                        or AgentSkillsProvider.ReadSkillResourceToolName
                        or AgentSkillsProvider.RunSkillScriptToolName,
                })];

                if (supportedApprovals.Count == 0)
                {
                    _ = session.StateBag.TryRemoveValue(PendingApprovalRequestsStateKey);
                }
                else if (supportedApprovals.Count != pendingApprovals.Count)
                {
                    session.StateBag.SetValue(
                        PendingApprovalRequestsStateKey,
                        supportedApprovals,
                        AgentSessionJsonOptions.Default);
                }
            }

            return session;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            logger.LogWarning(ex, "Discarded unreadable agent session state and started a new session. conversationId={ConversationId}", conversationId);

            return await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 序列化并保存指定会话的 Session 检查点。
    /// </summary>
    /// <param name="agent">承载本次运行的 Agent。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="session">待保存的 Session。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>按产品会话标识 upsert：命中则覆盖写入，未命中则新建状态行。</remarks>
    public async Task SaveSessionAsync(
        AIAgent agent,
        Guid conversationId,
        AgentSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(session);

        JsonElement serialized = await agent
            .SerializeSessionAsync(session, AgentSessionJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        string content = serialized.GetRawText();
        DateTimeOffset now = timeProvider.GetUtcNow();

        bool updated = await this._sessionStates
            .UpdateSessionState(conversationId, content, now, cancellationToken)
            .ConfigureAwait(false);

        if (updated)
        {
            return;
        }

        _ = await this._sessionStates.AddSessionState(
            new AgentSessionState
            {
                Id = Guid.CreateVersion7(),
                ConversationId = conversationId,
                SessionState = content,
                CreatedTime = now,
                UpdatedTime = now,
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 按产品会话标识删除状态行。
    /// </summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task DeleteSessionStateAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        _ = await this._sessionStates.DeleteSessionState(conversationId, cancellationToken).ConfigureAwait(false);
    }
}
