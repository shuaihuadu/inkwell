// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence;

namespace Inkwell;

/// <summary><see cref="IAuthService"/> 唯一实现；编排密码校验 / 会话缓存读写 / 失败计数。</summary>
internal sealed class AuthService(
    IPersistenceProvider persistenceProvider,
    ICacheProvider cacheProvider,
    IOptions<AuthOptions> authOptions,
    TimeProvider clock) : IAuthService
{
    /// <summary>账号不存在时仍执行一次哑校验，避免账号枚举计时侧信道。</summary>
    private const string DummyPasswordHash = "PBKDF2$600000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private readonly IUserRepository _users = persistenceProvider.GetRepository<IUserRepository>();

    public async Task<AuthSession> LoginAsync(string username, string password, string? clientIp = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentException.ThrowIfNullOrEmpty(password);

        User? user;

        try
        {
            user = await this._users.GetUserByUsername(username, ct).ConfigureAwait(false);
        }
        catch (KeyNotFoundException)
        {
            // 账号不存在时仍执行一次相同成本的 PBKDF2 校验，使其响应时间尽量接近
            // “账号存在但密码错误”的路径，降低攻击者根据耗时差异枚举有效账号的风险。
            // DummyPasswordHash 仅用于消耗等量计算时间，校验结果必须丢弃，随后统一返回登录失败。
            _ = PasswordHasher.Verify(password, DummyPasswordHash);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            bool nowLocked = await this.RegisterFailedPasswordAttemptAsync(user, ct).ConfigureAwait(false);

            throw nowLocked
                ? new InvalidOperationException("Account locked: too many failed login attempts.")
                : new UnauthorizedAccessException("Invalid username or password.");
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
            innerCt => this._users.UpdateUser(user with { LastLoginTime = now, FailedUnlockAttempts = 0 }, innerCt),
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

        User user = await this._users.GetUser(userId, ct).ConfigureAwait(false);

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            bool nowLocked = await this.RegisterFailedPasswordAttemptAsync(user, ct).ConfigureAwait(false);

            throw nowLocked
                ? new InvalidOperationException("Account locked: too many failed unlock attempts.")
                : new UnauthorizedAccessException("Invalid password.");
        }

        if (user.FailedUnlockAttempts != 0)
        {
            await persistenceProvider.ExecuteInTransactionAsync(
                innerCt => this._users.UpdateUser(user with { FailedUnlockAttempts = 0 }, innerCt),
                ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 记录一次密码校验失败（登录或解锁场景共用同一失败计数），达到阈值时锁定账号。
    /// </summary>
    /// <param name="user">校验失败对应的账号。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>账号是否因本次失败而进入锁定状态。</returns>
    private async Task<bool> RegisterFailedPasswordAttemptAsync(User user, CancellationToken ct)
    {
        int maxAttempts = authOptions.Value.MaxFailedUnlockAttempts;
        int newFailedAttempts = user.FailedUnlockAttempts + 1;
        bool nowLocked = user.IsLocked || newFailedAttempts >= maxAttempts;

        await persistenceProvider.ExecuteInTransactionAsync(
            innerCt => this._users.UpdateUser(user with { FailedUnlockAttempts = newFailedAttempts, IsLocked = nowLocked }, innerCt),
            ct).ConfigureAwait(false);

        return nowLocked;
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
            User user = await this._users.GetUser(targetUserId, innerCt).ConfigureAwait(false);

            await this._users.UpdateUser(user with { IsLocked = false, FailedUnlockAttempts = 0 }, innerCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserListItem>> ListAccountsAsync(bool? isLocked, CancellationToken ct = default)
    {
        if (isLocked is not null)
        {
            IReadOnlyList<User> filtered = await this._users.FindUsersByLockedStatus(isLocked.Value, ct).ConfigureAwait(false);

            return [.. filtered.Select(ToAccountSummary)];
        }

        List<User> all = await PaginationHelper.CollectAllAsync(
            (pagination, innerCt) => this._users.ListUsers(pagination, SortOrder.ByCreatedAtDesc, innerCt),
            ct).ConfigureAwait(false);

        return [.. all.Select(ToAccountSummary)];
    }

    private static UserListItem ToAccountSummary(User user) =>
        new(user.Id, user.Username, user.IsSuper, user.IsLocked, user.LastLoginTime, user.CreatedTime);

    private static string BuildSessionCacheKey(string token) => $"auth:session:{token}";
}
