using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell.Agents.Middleware;

/// <summary>
/// 会话持久化中间件
/// 在 Agent 运行前加载会话，运行后持久化会话状态和消息
/// </summary>
public static class SessionPersistenceMiddleware
{
    /// <summary>
    /// 创建包装后的 Agent，自动管理 AGUI 会话的加载与持久化
    /// </summary>
    /// <param name="agent">原始 Agent</param>
    /// <param name="agentId">Agent ID（用于持久化标识）</param>
    /// <param name="sessionProvider">会话持久化提供程序</param>
    /// <returns>包装后的 Agent</returns>
    public static AIAgent WithSessionPersistence(
        this AIAgent agent,
        string agentId,
        ISessionPersistenceProvider sessionProvider)
    {
        return agent
            .AsBuilder()
            .Use(
                CreateRunFunc(agentId, sessionProvider),
                CreateRunStreamingFunc(agentId, sessionProvider))
            .Build();
    }

    /// <summary>
    /// 从 AGUI 运行选项中提取 threadId
    /// </summary>
    private static string? ExtractThreadId(AgentRunOptions? options)
    {
        if (options is ChatClientAgentRunOptions runOptions
            && runOptions.ChatOptions?.AdditionalProperties is { } props
            && props.TryGetValue("ag_ui_thread_id", out object? threadIdObj)
            && threadIdObj is string threadId
            && !string.IsNullOrWhiteSpace(threadId))
        {
            return threadId;
        }

        return null;
    }

    /// <summary>
    /// 加载或创建 AgentSession
    /// </summary>
    private static async Task<(AgentSession session, string? threadId)> LoadOrCreateSessionAsync(
        AIAgent innerAgent,
        AgentRunOptions? options,
        ISessionPersistenceProvider sessionProvider,
        CancellationToken cancellationToken)
    {
        string? threadId = ExtractThreadId(options);

        if (threadId is null)
        {
            AgentSession newSession = await innerAgent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
            return (newSession, null);
        }

        JsonElement? savedState = await sessionProvider.LoadSessionAsync(threadId, cancellationToken).ConfigureAwait(false);

        if (savedState.HasValue)
        {
            AgentSession restored = await innerAgent.DeserializeSessionAsync(savedState.Value).ConfigureAwait(false);
            return (restored, threadId);
        }

        AgentSession created = await innerAgent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        return (created, threadId);
    }

    /// <summary>
    /// 持久化会话状态和消息
    /// </summary>
    private static async Task PersistSessionAsync(
        AIAgent innerAgent,
        AgentSession session,
        string agentId,
        string threadId,
        ISessionPersistenceProvider sessionProvider,
        CancellationToken cancellationToken)
    {
        JsonElement state = await innerAgent.SerializeSessionAsync(session).ConfigureAwait(false);
        await sessionProvider.SaveSessionAsync(threadId, agentId, state, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 创建非流式运行委托
    /// </summary>
    private static Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, AIAgent, CancellationToken, Task<AgentResponse>>
        CreateRunFunc(string agentId, ISessionPersistenceProvider sessionProvider)
    {
        return async (messages, session, options, innerAgent, cancellationToken) =>
        {
            (AgentSession loadedSession, string? threadId) =
                await LoadOrCreateSessionAsync(innerAgent, options, sessionProvider, cancellationToken).ConfigureAwait(false);

            AgentResponse response = await innerAgent.RunAsync(messages, loadedSession, options, cancellationToken).ConfigureAwait(false);

            if (threadId is not null)
            {
                await PersistSessionAsync(innerAgent, loadedSession, agentId, threadId, sessionProvider, cancellationToken).ConfigureAwait(false);
            }

            return response;
        };
    }

    /// <summary>
    /// 创建流式运行委托
    /// </summary>
    private static Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, AIAgent, CancellationToken, IAsyncEnumerable<AgentResponseUpdate>>
        CreateRunStreamingFunc(string agentId, ISessionPersistenceProvider sessionProvider)
    {
        return (messages, session, options, innerAgent, cancellationToken) =>
        {
            return RunStreamingWithPersistenceAsync(messages, options, innerAgent, agentId, sessionProvider, cancellationToken);
        };
    }

    /// <summary>
    /// 带持久化的流式运行
    /// </summary>
    private static async IAsyncEnumerable<AgentResponseUpdate> RunStreamingWithPersistenceAsync(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options,
        AIAgent innerAgent,
        string agentId,
        ISessionPersistenceProvider sessionProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        (AgentSession loadedSession, string? threadId) =
            await LoadOrCreateSessionAsync(innerAgent, options, sessionProvider, cancellationToken).ConfigureAwait(false);

        await foreach (AgentResponseUpdate update in innerAgent.RunStreamingAsync(messages, loadedSession, options, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }

        if (threadId is not null)
        {
            await PersistSessionAsync(innerAgent, loadedSession, agentId, threadId, sessionProvider, cancellationToken).ConfigureAwait(false);
        }
    }
}
