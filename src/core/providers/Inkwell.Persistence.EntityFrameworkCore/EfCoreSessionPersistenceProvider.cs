using System.Text.Json;
using Inkwell.Agents;
using Inkwell.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkwell.Persistence.EntityFrameworkCore;

/// <summary>
/// 基于 EF Core 的会话持久化提供程序
/// </summary>
public sealed class EfCoreSessionPersistenceProvider(InkwellDbContext dbContext) : ISessionPersistenceProvider
{
    /// <inheritdoc />
    public async Task SaveSessionAsync(string sessionId, string agentId, JsonElement sessionState, CancellationToken cancellationToken = default)
    {
        ChatSessionEntity? existing = await dbContext.ChatSessions
            .FindAsync([sessionId], cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            dbContext.ChatSessions.Add(new ChatSessionEntity
            {
                Id = sessionId,
                AgentId = agentId,
                SessionState = sessionState.GetRawText(),
                MessageCount = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            existing.SessionState = sessionState.GetRawText();
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<JsonElement?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ChatSessionEntity? entity = await dbContext.ChatSessions
            .FindAsync([sessionId], cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        return JsonDocument.Parse(entity.SessionState).RootElement;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListSessionsAsync(string agentId, CancellationToken cancellationToken = default)
    {
        List<string> ids = await dbContext.ChatSessions
            .Where(s => s.AgentId == agentId)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ids.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // 先删消息
        List<ChatMessageEntity> messages = await dbContext.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (messages.Count > 0)
        {
            dbContext.ChatMessages.RemoveRange(messages);
        }

        // 再删会话
        ChatSessionEntity? session = await dbContext.ChatSessions
            .FindAsync([sessionId], cancellationToken)
            .ConfigureAwait(false);

        if (session is not null)
        {
            dbContext.ChatSessions.Remove(session);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ChatSessionEntity? entity = await dbContext.ChatSessions
            .FindAsync([sessionId], cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        return new SessionInfo(entity.Id, entity.AgentId, entity.Title, entity.MessageCount, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SessionInfo>> ListSessionInfosAsync(string agentId, CancellationToken cancellationToken = default)
    {
        List<SessionInfo> infos = await dbContext.ChatSessions
            .Where(s => s.AgentId == agentId)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new SessionInfo(s.Id, s.AgentId, s.Title, s.MessageCount, s.CreatedAt, s.UpdatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return infos.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken cancellationToken = default)
    {
        ChatSessionEntity? entity = await dbContext.ChatSessions
            .FindAsync([sessionId], cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            entity.Title = title;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessageRecord> messages, CancellationToken cancellationToken = default)
    {
        List<ChatMessageEntity> entities = messages.Select(m => new ChatMessageEntity
        {
            Id = m.Id,
            SessionId = sessionId,
            Role = m.Role,
            Content = m.Content,
            Status = m.Status,
            CreatedAt = m.CreatedAt
        }).ToList();

        if (entities.Count > 0)
        {
            dbContext.ChatMessages.AddRange(entities);
        }

        // 更新消息计数
        ChatSessionEntity? session = await dbContext.ChatSessions
            .FindAsync([sessionId], cancellationToken)
            .ConfigureAwait(false);

        if (session is not null)
        {
            int totalCount = await dbContext.ChatMessages
                .CountAsync(m => m.SessionId == sessionId, cancellationToken)
                .ConfigureAwait(false);

            session.MessageCount = totalCount + entities.Count;
            session.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessageRecord>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        List<ChatMessageRecord> messages = await dbContext.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageRecord(m.Id, m.Role, m.Content, m.Status, m.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return messages.AsReadOnly();
    }
}
