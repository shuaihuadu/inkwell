using System.Collections.Concurrent;
using System.Text.Json;

namespace Inkwell.Agents;

/// <summary>
/// 内存会话持久化提供程序（开发用）
/// </summary>
public sealed class InMemorySessionPersistenceProvider : ISessionPersistenceProvider
{
    private readonly ConcurrentDictionary<string, SessionRecord> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<ChatMessageRecord>> _messages = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task SaveSessionAsync(string sessionId, string agentId, JsonElement sessionState, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        this._sessions.AddOrUpdate(
            sessionId,
            _ => new SessionRecord(sessionId, agentId, null, sessionState, 0, now, now),
            (_, existing) => existing with { State = sessionState, UpdatedAt = now });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<JsonElement?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (this._sessions.TryGetValue(sessionId, out SessionRecord? record))
        {
            return Task.FromResult<JsonElement?>(record.State);
        }

        return Task.FromResult<JsonElement?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListSessionsAsync(string agentId, CancellationToken cancellationToken = default)
    {
        List<string> sessionIds = this._sessions.Values
            .Where(r => r.AgentId.Equals(agentId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => r.SessionId)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(sessionIds.AsReadOnly());
    }

    /// <inheritdoc />
    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        this._sessions.TryRemove(sessionId, out _);
        this._messages.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (this._sessions.TryGetValue(sessionId, out SessionRecord? record))
        {
            return Task.FromResult<SessionInfo?>(new SessionInfo(
                record.SessionId, record.AgentId, record.Title,
                record.MessageCount, record.CreatedAt, record.UpdatedAt));
        }

        return Task.FromResult<SessionInfo?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SessionInfo>> ListSessionInfosAsync(string agentId, CancellationToken cancellationToken = default)
    {
        List<SessionInfo> infos = this._sessions.Values
            .Where(r => r.AgentId.Equals(agentId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => new SessionInfo(r.SessionId, r.AgentId, r.Title, r.MessageCount, r.CreatedAt, r.UpdatedAt))
            .ToList();

        return Task.FromResult<IReadOnlyList<SessionInfo>>(infos.AsReadOnly());
    }

    /// <inheritdoc />
    public Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken cancellationToken = default)
    {
        if (this._sessions.TryGetValue(sessionId, out SessionRecord? record))
        {
            this._sessions[sessionId] = record with { Title = title, UpdatedAt = DateTimeOffset.UtcNow };
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessageRecord> messages, CancellationToken cancellationToken = default)
    {
        List<ChatMessageRecord> newMessages = messages.ToList();

        this._messages.AddOrUpdate(
            sessionId,
            _ => newMessages,
            (_, existing) => { existing.AddRange(newMessages); return existing; });

        // 更新消息计数
        if (this._sessions.TryGetValue(sessionId, out SessionRecord? record))
        {
            int totalCount = this._messages.TryGetValue(sessionId, out List<ChatMessageRecord>? msgs) ? msgs.Count : 0;
            this._sessions[sessionId] = record with { MessageCount = totalCount, UpdatedAt = DateTimeOffset.UtcNow };
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ChatMessageRecord>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (this._messages.TryGetValue(sessionId, out List<ChatMessageRecord>? messages))
        {
            return Task.FromResult<IReadOnlyList<ChatMessageRecord>>(
                messages.OrderBy(m => m.CreatedAt).ToList().AsReadOnly());
        }

        return Task.FromResult<IReadOnlyList<ChatMessageRecord>>(Array.Empty<ChatMessageRecord>());
    }

    private sealed record SessionRecord(
        string SessionId,
        string AgentId,
        string? Title,
        JsonElement State,
        int MessageCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
