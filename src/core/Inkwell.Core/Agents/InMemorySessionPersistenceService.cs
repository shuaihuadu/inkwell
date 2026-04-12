using System.Collections.Concurrent;
using System.Text.Json;

namespace Inkwell.Agents;

/// <summary>
/// 内存会话持久化服务（开发用）
/// </summary>
public sealed class InMemorySessionPersistenceService : ISessionPersistenceService
{
    private readonly ConcurrentDictionary<string, SessionRecord> _sessions = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task SaveSessionAsync(string sessionId, string agentId, JsonElement sessionState, CancellationToken cancellationToken = default)
    {
        this._sessions[sessionId] = new SessionRecord(sessionId, agentId, sessionState, DateTime.UtcNow);
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
        return Task.CompletedTask;
    }

    private sealed record SessionRecord(string SessionId, string AgentId, JsonElement State, DateTime UpdatedAt);
}
