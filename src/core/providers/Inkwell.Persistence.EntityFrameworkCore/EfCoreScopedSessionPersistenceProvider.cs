using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell.Persistence.EntityFrameworkCore;

/// <summary>
/// 基于 Scope 的会话持久化提供程序包装器
/// 每次操作创建新的 scope 获取 DbContext，适合在 Singleton 上下文中使用
/// </summary>
public sealed class EfCoreScopedSessionPersistenceProvider(IServiceScopeFactory scopeFactory) : ISessionPersistenceProvider
{
    private EfCoreSessionPersistenceProvider CreateProvider(IServiceScope scope)
    {
        InkwellDbContext db = scope.ServiceProvider.GetRequiredService<InkwellDbContext>();
        return new EfCoreSessionPersistenceProvider(db);
    }

    /// <inheritdoc />
    public async Task SaveSessionAsync(string sessionId, string agentId, JsonElement sessionState, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        await this.CreateProvider(scope).SaveSessionAsync(sessionId, agentId, sessionState, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<JsonElement?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        return await this.CreateProvider(scope).LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListSessionsAsync(string agentId, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        return await this.CreateProvider(scope).ListSessionsAsync(agentId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        await this.CreateProvider(scope).DeleteSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        return await this.CreateProvider(scope).GetSessionInfoAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SessionInfo>> ListSessionInfosAsync(string agentId, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        return await this.CreateProvider(scope).ListSessionInfosAsync(agentId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        await this.CreateProvider(scope).UpdateSessionTitleAsync(sessionId, title, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessageRecord> messages, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        await this.CreateProvider(scope).SaveMessagesAsync(sessionId, messages, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessageRecord>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        return await this.CreateProvider(scope).GetMessagesAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }
}
