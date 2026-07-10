using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary><see cref="IAuthService"/> 唯一实现；编排密码校验 / 会话缓存读写 / 失败计数。</summary>
internal sealed class AuthService(
    IUserRepository users,
    IPersistenceProvider persistenceProvider,
    ICacheProvider cacheProvider,
    IOptions<AuthOptions> authOptions,
    TimeProvider clock) : IAuthService
{
    /// <summary>账号不存在时仍执行一次哑校验，避免账号枚举计时侧信道。</summary>
    private const string DummyPasswordHash = "PBKDF2$600000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    public async Task<AuthSession> LoginAsync(string username, string password, string? clientIp = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentException.ThrowIfNullOrEmpty(password);

        User? user;

        try
        {
            user = await users.GetUserByUsername(username, ct).ConfigureAwait(false);
        }
        catch (KeyNotFoundException)
        {
            // 假验证：与真实路径耗时对齐，避免计时侧信道账号枚举。
            _ = PasswordHasher.Verify(password, DummyPasswordHash);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (user.IsLocked)
        {
            throw new InvalidOperationException("Account locked: contact administrator.");
        }

        DateTimeOffset now = clock.GetUtcNow();
        string token = SessionTokenGenerator.Generate();
        AuthOptions options = authOptions.Value;

        await cacheProvider.SetAsync(
            BuildSessionCacheKey(token),
            new SessionCacheEntry(user.Id, user.Username, user.IsSuper, now),
            new CacheEntryOptions(TimeSpan.FromHours(options.SessionTtlHours)),
            ct).ConfigureAwait(false);

        await persistenceProvider.ExecuteInTransactionAsync(
            innerCt => users.UpdateUser(user with { LastLoginTime = now }, innerCt),
            ct).ConfigureAwait(false);

        return new AuthSession(user.Id, user.Username, user.IsSuper, token, now.AddHours(options.SessionTtlHours));
    }

    public async Task<bool> LogoutAsync(string sessionToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionToken);

        string key = BuildSessionCacheKey(sessionToken);
        SessionCacheEntry? entry = await cacheProvider.GetAsync<SessionCacheEntry>(key, ct).ConfigureAwait(false);

        if (entry is null)
        {
            return false;
        }

        await cacheProvider.RemoveAsync(key, ct).ConfigureAwait(false);

        return true;
    }

    public async Task<AuthSession> ValidateSessionAsync(string sessionToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionToken);

        SessionCacheEntry? entry = await cacheProvider.GetAsync<SessionCacheEntry>(BuildSessionCacheKey(sessionToken), ct).ConfigureAwait(false);

        if (entry is null)
        {
            throw new UnauthorizedAccessException("Session expired or invalid.");
        }

        DateTimeOffset expiresAt = entry.IssuedAt.AddHours(authOptions.Value.SessionTtlHours);

        return new AuthSession(entry.UserId, entry.Username, entry.IsSuper, sessionToken, expiresAt);
    }

    public async Task VerifyPasswordForUnlockAsync(Guid userId, string password, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        User user = await users.GetUser(userId, ct).ConfigureAwait(false);

        int maxAttempts = authOptions.Value.MaxFailedUnlockAttempts;
        bool passwordCorrect = PasswordHasher.Verify(password, user.PasswordHash);
        int newFailedAttempts = passwordCorrect ? 0 : user.FailedUnlockAttempts + 1;
        bool nowLocked = !passwordCorrect && newFailedAttempts >= maxAttempts;

        await persistenceProvider.ExecuteInTransactionAsync(
            innerCt => users.UpdateUser(user with { FailedUnlockAttempts = newFailedAttempts, IsLocked = user.IsLocked || nowLocked }, innerCt),
            ct).ConfigureAwait(false);

        if (!passwordCorrect)
        {
            if (nowLocked)
            {
                throw new InvalidOperationException("Account locked: too many failed unlock attempts.");
            }

            throw new UnauthorizedAccessException("Invalid password.");
        }
    }

    public async Task UnlockAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default)
    {
        if (targetUserId == Guid.Empty)
        {
            throw new ArgumentException("TargetUserId must not be empty.", nameof(targetUserId));
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("ActorUserId must not be empty.", nameof(actorUserId));
        }

        await persistenceProvider.ExecuteInTransactionAsync(async innerCt =>
        {
            User user = await users.GetUser(targetUserId, innerCt).ConfigureAwait(false);

            await users.UpdateUser(user with { IsLocked = false, FailedUnlockAttempts = 0 }, innerCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuthAccountSummary>> ListAccountsAsync(bool? isLocked, CancellationToken ct = default)
    {
        if (isLocked is not null)
        {
            IReadOnlyList<User> filtered = await users.FindUsersByLockedStatus(isLocked.Value, ct).ConfigureAwait(false);

            return [.. filtered.Select(ToAccountSummary)];
        }

        List<User> all = await PaginationHelper.CollectAllAsync(
            (pagination, innerCt) => users.ListUsers(pagination, SortOrder.ByCreatedAtDesc, innerCt),
            ct).ConfigureAwait(false);

        return [.. all.Select(ToAccountSummary)];
    }

    private static AuthAccountSummary ToAccountSummary(User user) =>
        new(user.Id, user.Username, user.IsSuper, user.IsLocked, user.LastLoginTime, user.CreatedTime);

    private static string BuildSessionCacheKey(string token) => $"auth:session:{token}";
}
