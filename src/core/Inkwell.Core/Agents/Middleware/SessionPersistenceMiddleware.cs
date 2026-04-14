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
    /// <param name="titleGenerator">用于生成会话标题的 LLM 客户端（可选，不传则截取首条消息前 50 字符）</param>
    /// <returns>包装后的 Agent</returns>
    public static AIAgent WithSessionPersistence(
        this AIAgent agent,
        string agentId,
        ISessionPersistenceProvider sessionProvider,
        IChatClient? titleGenerator = null)
    {
        return agent
            .AsBuilder()
            .Use(
                CreateRunFunc(agentId, sessionProvider, titleGenerator),
                CreateRunStreamingFunc(agentId, sessionProvider, titleGenerator))
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
        IEnumerable<ChatMessage> inputMessages,
        ISessionPersistenceProvider sessionProvider,
        IChatClient? titleGenerator,
        CancellationToken cancellationToken)
    {
        // 1) 序列化并保存 session state
        JsonElement state = await innerAgent.SerializeSessionAsync(session).ConfigureAwait(false);
        await sessionProvider.SaveSessionAsync(threadId, agentId, state, cancellationToken).ConfigureAwait(false);

        // 2) 保存用户输入消息到 ChatMessageRecord
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<ChatMessageRecord> newMessages = [];
        string? firstUserMessage = null;

        foreach (ChatMessage msg in inputMessages)
        {
            string content = msg.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            newMessages.Add(new ChatMessageRecord(
                Guid.NewGuid().ToString("N"),
                msg.Role.Value,
                content,
                "done",
                now));

            if (msg.Role == ChatRole.User && firstUserMessage is null)
            {
                firstUserMessage = content;
            }
        }

        // 3) 从 session 的 ChatHistoryProvider 中提取最新的 assistant 回复
        if (session.TryGetInMemoryChatHistory(out List<ChatMessage>? history) && history.Count > 0)
        {
            // 取最后一条 assistant 消息
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (history[i].Role == ChatRole.Assistant)
                {
                    string assistantContent = history[i].Text ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(assistantContent))
                    {
                        newMessages.Add(new ChatMessageRecord(
                            Guid.NewGuid().ToString("N"),
                            "assistant",
                            assistantContent,
                            "done",
                            now.AddMilliseconds(1)));
                    }

                    break;
                }
            }
        }

        if (newMessages.Count > 0)
        {
            await sessionProvider.SaveMessagesAsync(threadId, newMessages, cancellationToken).ConfigureAwait(false);
        }

        // 4) 首次对话时自动生成标题
        if (firstUserMessage is not null)
        {
            SessionInfo? info = await sessionProvider.GetSessionInfoAsync(threadId, cancellationToken).ConfigureAwait(false);
            if (info is not null && string.IsNullOrWhiteSpace(info.Title))
            {
                string title = await GenerateTitleAsync(firstUserMessage, titleGenerator, cancellationToken).ConfigureAwait(false);
                await sessionProvider.UpdateSessionTitleAsync(threadId, title, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// 生成会话标题：优先使用 LLM 总结，回退到截取前 50 字符
    /// </summary>
    private static async Task<string> GenerateTitleAsync(
        string firstUserMessage,
        IChatClient? titleGenerator,
        CancellationToken cancellationToken)
    {
        if (titleGenerator is not null)
        {
            try
            {
                ChatResponse response = await titleGenerator.GetResponseAsync(
                    [
                        new ChatMessage(ChatRole.System, "用不超过 15 个中文字为以下对话生成一个简短标题，只输出标题文字，不要加引号或标点。"),
                        new ChatMessage(ChatRole.User, firstUserMessage)
                    ],
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                string? generated = response.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(generated) && generated.Length <= 50)
                {
                    return generated;
                }
            }
            catch
            {
                // LLM 调用失败时回退到截取
            }
        }

        return firstUserMessage.Length > 50
            ? firstUserMessage[..50] + "..."
            : firstUserMessage;
    }

    /// <summary>
    /// 创建非流式运行委托
    /// </summary>
    private static Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, AIAgent, CancellationToken, Task<AgentResponse>>
        CreateRunFunc(string agentId, ISessionPersistenceProvider sessionProvider, IChatClient? titleGenerator)
    {
        return async (messages, session, options, innerAgent, cancellationToken) =>
        {
            (AgentSession loadedSession, string? threadId) =
                await LoadOrCreateSessionAsync(innerAgent, options, sessionProvider, cancellationToken).ConfigureAwait(false);

            AgentResponse response = await innerAgent.RunAsync(messages, loadedSession, options, cancellationToken).ConfigureAwait(false);

            if (threadId is not null)
            {
                await PersistSessionAsync(innerAgent, loadedSession, agentId, threadId, messages, sessionProvider, titleGenerator, cancellationToken).ConfigureAwait(false);
            }

            return response;
        };
    }

    /// <summary>
    /// 创建流式运行委托
    /// </summary>
    private static Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, AIAgent, CancellationToken, IAsyncEnumerable<AgentResponseUpdate>>
        CreateRunStreamingFunc(string agentId, ISessionPersistenceProvider sessionProvider, IChatClient? titleGenerator)
    {
        return (messages, session, options, innerAgent, cancellationToken) =>
        {
            return RunStreamingWithPersistenceAsync(messages, options, innerAgent, agentId, sessionProvider, titleGenerator, cancellationToken);
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
        IChatClient? titleGenerator,
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
            await PersistSessionAsync(innerAgent, loadedSession, agentId, threadId, messages, sessionProvider, titleGenerator, cancellationToken).ConfigureAwait(false);
        }
    }
}
