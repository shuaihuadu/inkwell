// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence;
using Microsoft.Extensions.Options;

namespace Inkwell.Core.Tests.Auth;

/// <summary>验证账号管理、密码更新和会话撤销规则。</summary>
[TestClass]
public sealed class AuthServiceTests
{
    /// <summary>验证管理员禁用账号后目标账号的既有会话立即失效。</summary>
    [TestMethod]
    public async Task DisableAccountAsync_InvalidatesExistingSessionAsync()
    {
        // Arrange
        User administrator = CreateUser("admin", "Admin-password!", isAdmin: true);
        User member = CreateUser("member", "Member-password!", isAdmin: false);
        AuthService service = CreateService([administrator, member]);
        AuthSession session = await service.LoginAsync(member.Username, "Member-password!");

        // Act
        await service.DisableAccountAsync(member.Id, administrator.Id);
        Task ActAsync() => service.ValidateSessionAsync(session.SessionToken);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(ActAsync);
    }

    /// <summary>验证禁用再启用账号不会隐式解除安全策略锁定。</summary>
    [TestMethod]
    public async Task EnableAccountAsync_PreservesAutomaticLockAsync()
    {
        // Arrange
        User administrator = CreateUser("admin", "Admin-password!", isAdmin: true);
        User member = CreateUser("member", "Member-password!", isAdmin: false) with
        {
            IsLocked = true,
            FailedUnlockAttempts = 5,
        };
        AuthService service = CreateService([administrator, member]);

        // Act
        await service.DisableAccountAsync(member.Id, administrator.Id);
        await service.EnableAccountAsync(member.Id, administrator.Id);
        IReadOnlyList<UserListItem> accounts = await service.ListAccountsAsync(administrator.Id);
        UserListItem updated = accounts.Single(account => account.UserId == member.Id);

        // Assert
        Assert.IsFalse(updated.IsDisabled);
        Assert.IsTrue(updated.IsLocked);
    }

    /// <summary>验证账号锁定仅由密码验证失败达到安全阈值触发。</summary>
    [TestMethod]
    public async Task LoginAsync_WhenFailureThresholdReached_AutomaticallyLocksAccountAsync()
    {
        // Arrange
        User administrator = CreateUser("admin", "Admin-password!", isAdmin: true);
        User member = CreateUser("member", "Member-password!", isAdmin: false);
        AuthService service = CreateService(
            [administrator, member],
            new AuthOptions { MaxFailedUnlockAttempts = 1 });

        // Act
        Task ActAsync() => service.LoginAsync(member.Username, "Wrong-password!");
        await Assert.ThrowsAsync<InvalidOperationException>(ActAsync);
        IReadOnlyList<UserListItem> accounts = await service.ListAccountsAsync(administrator.Id);
        UserListItem updated = accounts.Single(account => account.UserId == member.Id);

        // Assert
        Assert.IsTrue(updated.IsLocked);
        Assert.IsFalse(updated.IsDisabled);
    }

    /// <summary>验证改密保留当前会话并撤销同账号的其他会话。</summary>
    [TestMethod]
    public async Task ChangePasswordAsync_PreservesCurrentSessionAndInvalidatesOtherSessionsAsync()
    {
        // Arrange
        User member = CreateUser("member", "Member-password!", isAdmin: false);
        AuthService service = CreateService([member]);
        AuthSession currentSession = await service.LoginAsync(member.Username, "Member-password!");
        AuthSession otherSession = await service.LoginAsync(member.Username, "Member-password!");

        // Act
        AuthSession changedSession = await service.ChangePasswordAsync(
            member.Id,
            "Member-password!",
            "Changed-password!",
            currentSession.SessionToken);
        AuthSession validatedCurrent = await service.ValidateSessionAsync(currentSession.SessionToken);
        Task ValidateOtherAsync() => service.ValidateSessionAsync(otherSession.SessionToken);

        // Assert
        Assert.IsFalse(changedSession.MustChangePassword);
        Assert.AreEqual(currentSession.SessionToken, validatedCurrent.SessionToken);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(ValidateOtherAsync);
    }

    /// <summary>验证新账号临时密码可登录且登录后必须修改密码。</summary>
    [TestMethod]
    public async Task CreateAccountAsync_IssuesTemporaryPasswordThatRequiresChangeAsync()
    {
        // Arrange
        User administrator = CreateUser("admin", "Admin-password!", isAdmin: true);
        AuthService service = CreateService([administrator]);

        // Act
        IssuedCredential credential = await service.CreateAccountAsync(
            "new-member",
            isAdmin: false,
            administrator.Id);
        AuthSession session = await service.LoginAsync(
            credential.Username,
            credential.TemporaryPassword);

        // Assert
        Assert.AreEqual("new-member", credential.Username);
        Assert.IsTrue(session.MustChangePassword);
    }

    private static AuthService CreateService(IEnumerable<User> users, AuthOptions? options = null) =>
        new(
            new ImmediatePersistenceProvider(new InMemoryUserRepository(users)),
            new InMemoryCacheProvider(),
            Options.Create(options ?? new AuthOptions()),
            TimeProvider.System);

    private static User CreateUser(string username, string password, bool isAdmin)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new User
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            IsAdmin = isAdmin,
            IsLocked = false,
            IsDisabled = false,
            MustChangePassword = false,
            SessionVersion = 0,
            FailedUnlockAttempts = 0,
            LastLoginTime = null,
            CreatedTime = now,
            UpdatedTime = now,
        };
    }

    private sealed class InMemoryUserRepository(IEnumerable<User> users) : IUserRepository
    {
        private readonly Dictionary<Guid, User> _users = users.ToDictionary(user => user.Id);

        public Task<User> AddUser(User user, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (this._users.Values.Any(existing => string.Equals(existing.Username, user.Username, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Duplicate username.");
            }

            this._users.Add(user.Id, user);
            return Task.FromResult(user);
        }

        public Task UpdateUser(User user, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            this._users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task<User> GetUser(Guid id, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(this._users.TryGetValue(id, out User? user) ? user : throw new KeyNotFoundException());
        }

        public Task<User> GetUserByUsername(string username, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            User? user = this._users.Values.FirstOrDefault(candidate => candidate.Username == username);
            return Task.FromResult(user ?? throw new KeyNotFoundException());
        }

        public Task<PagedResult<User>> ListUsers(Pagination pagination, SortOrder sort, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<User> items = [.. this._users.Values];
            return Task.FromResult(new PagedResult<User>(items, items.Count, pagination));
        }

        public Task<IReadOnlyList<User>> FindUsersByLockedStatus(bool isLocked, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<User> users = [.. this._users.Values.Where(user => user.IsLocked == isLocked)];
            return Task.FromResult(users);
        }
    }

    private sealed class ImmediatePersistenceProvider(IUserRepository users) : IPersistenceProvider
    {
        public TRepository GetRepository<TRepository>() where TRepository : notnull =>
            users is TRepository repository ? repository : throw new NotSupportedException();

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) => action(ct);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) => action(ct);

        public Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct = default) => action(ct);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) => action(ct);
    }

    private sealed class InMemoryCacheProvider : ICacheProvider
    {
        private readonly Dictionary<string, object> _entries = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(this._entries.TryGetValue(key, out object? value) ? (T?)value : default);
        }

        public Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            this._entries[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _ = this._entries.Remove(key);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default) => Task.FromResult(this._entries.ContainsKey(key));

        public Task<long> IncrementAsync(string key, long delta, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> TryAcquireLockAsync(string key, TimeSpan ttl, CancellationToken ct = default) => throw new NotSupportedException();

        public Task ReleaseLockAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
