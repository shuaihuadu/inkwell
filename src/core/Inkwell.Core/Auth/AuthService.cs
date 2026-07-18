// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Cryptography;
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

        if (user.IsDisabled)
        {
            throw new InvalidOperationException("Account disabled: contact administrator.");
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
            new SessionCacheEntry(user.Id, user.Username, user.IsAdmin, user.MustChangePassword, user.SessionVersion, now),
            new CacheEntryOptions(TimeSpan.FromHours(options.SessionTtlHours)),
            ct).ConfigureAwait(false);

        await persistenceProvider.ExecuteInTransactionAsync(
            innerCt => this._users.UpdateUser(user with { LastLoginTime = now, FailedUnlockAttempts = 0 }, innerCt),
            ct).ConfigureAwait(false);

        return new AuthSession(user.Id, user.Username, user.IsAdmin, user.MustChangePassword, token, now.AddHours(options.SessionTtlHours));
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

        User user = await this._users.GetUser(entry.UserId, ct).ConfigureAwait(false);

        if (user.IsLocked || user.IsDisabled || user.SessionVersion != entry.SessionVersion)
        {
            await cacheProvider.RemoveAsync(BuildSessionCacheKey(sessionToken), ct).ConfigureAwait(false);
            throw new UnauthorizedAccessException("Session expired or invalid.");
        }

        DateTimeOffset expiresAt = entry.IssuedAt.AddHours(authOptions.Value.SessionTtlHours);

        return new AuthSession(entry.UserId, entry.Username, entry.IsAdmin, user.MustChangePassword, sessionToken, expiresAt);
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

    public async Task<AuthSession> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        string sessionToken,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(currentPassword);
        ValidateNewPassword(newPassword, currentPassword);
        ArgumentException.ThrowIfNullOrEmpty(sessionToken);

        User user = await this._users.GetUser(userId, ct).ConfigureAwait(false);

        if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid password.");
        }

        DateTimeOffset now = clock.GetUtcNow();
        User updated = user with
        {
            PasswordHash = PasswordHasher.Hash(newPassword),
            MustChangePassword = false,
            FailedUnlockAttempts = 0,
            SessionVersion = checked(user.SessionVersion + 1),
            UpdatedTime = now,
        };

        await persistenceProvider.ExecuteInTransactionAsync(
            innerCt => this._users.UpdateUser(updated, innerCt),
            ct).ConfigureAwait(false);

        AuthOptions options = authOptions.Value;
        SessionCacheEntry entry = new(updated.Id, updated.Username, updated.IsAdmin, false, updated.SessionVersion, now);
        await cacheProvider.SetAsync(
            BuildSessionCacheKey(sessionToken),
            entry,
            new CacheEntryOptions(TimeSpan.FromHours(options.SessionTtlHours)),
            ct).ConfigureAwait(false);

        return new AuthSession(updated.Id, updated.Username, updated.IsAdmin, false, sessionToken, now.AddHours(options.SessionTtlHours));
    }

    public async Task<IssuedCredential> CreateAccountAsync(
        string username,
        bool isAdmin,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        string normalizedUsername = username.Trim();
        if (normalizedUsername.Length is < 1 or > 100)
        {
            throw new ArgumentException("Username length must be between 1 and 100 characters.", nameof(username));
        }

        await this.RequireAdminAsync(actorUserId, ct).ConfigureAwait(false);

        string temporaryPassword = GenerateTemporaryPassword();
        DateTimeOffset now = clock.GetUtcNow();
        User created = await persistenceProvider.ExecuteInTransactionAsync(
            innerCt => this._users.AddUser(new User
            {
                Id = Guid.CreateVersion7(),
                Username = normalizedUsername,
                PasswordHash = PasswordHasher.Hash(temporaryPassword),
                IsAdmin = isAdmin,
                IsLocked = false,
                IsDisabled = false,
                MustChangePassword = true,
                SessionVersion = 0,
                FailedUnlockAttempts = 0,
                LastLoginTime = null,
                CreatedTime = now,
                UpdatedTime = now,
            }, innerCt),
            ct).ConfigureAwait(false);

        return new IssuedCredential(created.Id, created.Username, temporaryPassword);
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
        await this.UpdateAccountStateAsync(targetUserId, actorUserId, AccountStateAction.Unlock, ct).ConfigureAwait(false);
    }

    public Task DisableAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default) =>
        this.UpdateAccountStateAsync(targetUserId, actorUserId, AccountStateAction.Disable, ct);

    public Task EnableAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default) =>
        this.UpdateAccountStateAsync(targetUserId, actorUserId, AccountStateAction.Enable, ct);

    public async Task<IssuedCredential> ResetPasswordAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default)
    {
        await this.RequireAdminAsync(actorUserId, ct).ConfigureAwait(false);
        string temporaryPassword = GenerateTemporaryPassword();
        User updated = await persistenceProvider.ExecuteInTransactionAsync(async innerCt =>
        {
            User user = await this._users.GetUser(targetUserId, innerCt).ConfigureAwait(false);
            User next = user with
            {
                PasswordHash = PasswordHasher.Hash(temporaryPassword),
                MustChangePassword = true,
                FailedUnlockAttempts = 0,
                SessionVersion = checked(user.SessionVersion + 1),
                UpdatedTime = clock.GetUtcNow(),
            };
            await this._users.UpdateUser(next, innerCt).ConfigureAwait(false);
            return next;
        }, ct).ConfigureAwait(false);

        return new IssuedCredential(updated.Id, updated.Username, temporaryPassword);
    }

    public async Task<IReadOnlyList<UserListItem>> ListAccountsAsync(Guid actorUserId, CancellationToken ct = default)
    {
        await this.RequireAdminAsync(actorUserId, ct).ConfigureAwait(false);

        List<User> all = await PaginationHelper.CollectAllAsync(
            (pagination, innerCt) => this._users.ListUsers(pagination, SortOrder.ByCreatedAtDesc, innerCt),
            ct).ConfigureAwait(false);

        return [.. all.Select(ToAccountSummary)];
    }

    private static UserListItem ToAccountSummary(User user) =>
        new(user.Id, user.Username, user.IsAdmin, user.IsLocked, user.IsDisabled, user.LastLoginTime, user.CreatedTime);

    private async Task UpdateAccountStateAsync(
        Guid targetUserId,
        Guid actorUserId,
        AccountStateAction action,
        CancellationToken ct)
    {
        if (targetUserId == Guid.Empty)
        {
            throw new ArgumentException("TargetUserId must not be empty.", nameof(targetUserId));
        }

        await this.RequireAdminAsync(actorUserId, ct).ConfigureAwait(false);

        if (targetUserId == actorUserId && action == AccountStateAction.Disable)
        {
            throw new InvalidOperationException("The current account cannot be disabled.");
        }

        await persistenceProvider.ExecuteInTransactionAsync(async innerCt =>
        {
            User user = await this._users.GetUser(targetUserId, innerCt).ConfigureAwait(false);
            User updated = action switch
            {
                AccountStateAction.Unlock when user.IsDisabled => throw new InvalidOperationException("A disabled account cannot be unlocked."),
                AccountStateAction.Unlock => user with { IsLocked = false, FailedUnlockAttempts = 0 },
                AccountStateAction.Disable => user with { IsDisabled = true },
                AccountStateAction.Enable => user with { IsDisabled = false },
                _ => throw new ArgumentOutOfRangeException(nameof(action)),
            };

            updated = updated with
            {
                SessionVersion = checked(user.SessionVersion + 1),
                UpdatedTime = clock.GetUtcNow(),
            };
            await this._users.UpdateUser(updated, innerCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    private async Task RequireAdminAsync(Guid actorUserId, CancellationToken ct)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("ActorUserId must not be empty.", nameof(actorUserId));
        }

        User actor = await this._users.GetUser(actorUserId, ct).ConfigureAwait(false);
        if (!actor.IsAdmin || actor.IsLocked || actor.IsDisabled)
        {
            throw new UnauthorizedAccessException("Super user authorization is required.");
        }
    }

    private static void ValidateNewPassword(string newPassword, string currentPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(newPassword);

        if (newPassword.Length is < 8 or > 128)
        {
            throw new ArgumentException("Password length must be between 8 and 128 characters.", nameof(newPassword));
        }

        if (string.Equals(newPassword, currentPassword, StringComparison.Ordinal))
        {
            throw new ArgumentException("New password must differ from the current password.", nameof(newPassword));
        }
    }

    private static string GenerateTemporaryPassword() =>
        $"Ink-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant()}!";

    private static string BuildSessionCacheKey(string token) => $"auth:session:{token}";

    private enum AccountStateAction
    {
        Unlock,
        Disable,
        Enable,
    }
}
