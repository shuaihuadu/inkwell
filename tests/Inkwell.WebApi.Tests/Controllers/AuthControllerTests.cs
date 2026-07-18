// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Claims;
using System.Reflection;
using Inkwell.WebApi.Auth;
using Inkwell.WebApi.Controllers;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>
/// 验证认证 API 的解锁行为。
/// </summary>
[TestClass]
public sealed class AuthControllerTests
{
    private static readonly Guid userId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    /// <summary>
    /// 验证正确密码解锁成功。
    /// </summary>
    [TestMethod]
    public async Task UnlockAsync_WithValidPassword_ReturnsNoContentAsync()
    {
        // Arrange
        StubAuthService authService = new();
        AuthController controller = CreateController(authService);

        // Act
        IActionResult result = await controller.UnlockAsync(new UnlockRequest("correct"), CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<NoContentResult>(result);
        Assert.AreEqual(userId, authService.VerifiedUserId);
    }

    /// <summary>
    /// 验证错误密码返回未授权。
    /// </summary>
    [TestMethod]
    public async Task UnlockAsync_WithInvalidPassword_ReturnsUnauthorizedAsync()
    {
        // Arrange
        StubAuthService authService = new() { VerifyException = new UnauthorizedAccessException() };
        AuthController controller = CreateController(authService);

        // Act
        IActionResult result = await controller.UnlockAsync(new UnlockRequest("incorrect"), CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// 验证达到失败阈值时返回账号锁定。
    /// </summary>
    [TestMethod]
    public async Task UnlockAsync_WhenAccountLocks_ReturnsLockedAsync()
    {
        // Arrange
        StubAuthService authService = new() { VerifyException = new InvalidOperationException() };
        AuthController controller = CreateController(authService);

        // Act
        IActionResult result = await controller.UnlockAsync(new UnlockRequest("incorrect"), CancellationToken.None);

        // Assert
        StatusCodeResult lockedResult = Assert.IsInstanceOfType<StatusCodeResult>(result);
        Assert.AreEqual(StatusCodes.Status423Locked, lockedResult.StatusCode);
    }

    /// <summary>
    /// 验证账号列表筛选条件传递给认证服务。
    /// </summary>
    [TestMethod]
    public async Task ListAccountsAsync_ReturnsServiceAccountsAsync()
    {
        // Arrange
        UserListItem account = new(userId, "admin", true, true, false, null, DateTimeOffset.UtcNow);
        StubAuthService authService = new() { Accounts = [account] };
        AuthController controller = CreateController(authService);

        // Act
        ActionResult<IReadOnlyList<UserListItem>> result = await controller.ListAccountsAsync(CancellationToken.None);
        OkObjectResult okResult = (OkObjectResult)result.Result!;
        IReadOnlyList<UserListItem> accounts = (IReadOnlyList<UserListItem>)okResult.Value!;

        // Assert
        Assert.AreSame(authService.Accounts, accounts);
        Assert.AreEqual(userId, authService.ListAccountsActorUserId);
    }

    /// <summary>
    /// 验证解封账号使用路由目标和当前管理员标识。
    /// </summary>
    [TestMethod]
    public async Task UnlockAccountAsync_UsesCurrentAdministratorAsync()
    {
        // Arrange
        Guid targetUserId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        StubAuthService authService = new();
        AuthController controller = CreateController(authService);

        // Act
        IActionResult result = await controller.UnlockAccountAsync(targetUserId, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<NoContentResult>(result);
        Assert.AreEqual(targetUserId, authService.UnlockedUserId);
        Assert.AreEqual(userId, authService.UnlockActorUserId);
    }

    /// <summary>
    /// 验证账号管理端点要求管理员策略，且不提供管理员主动锁定端点。
    /// </summary>
    [TestMethod]
    public void AccountManagementEndpoints_RequireAdminPolicy()
    {
        // Arrange
        MethodInfo listMethod = typeof(AuthController).GetMethod(nameof(AuthController.ListAccountsAsync))!;
        MethodInfo unlockMethod = typeof(AuthController).GetMethod(nameof(AuthController.UnlockAccountAsync))!;
        MethodInfo? lockMethod = typeof(AuthController).GetMethod("LockAccountAsync");

        // Act
        string? listPolicy = listMethod.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
        string? unlockPolicy = unlockMethod.GetCustomAttribute<AuthorizeAttribute>()?.Policy;

        // Assert
        Assert.AreEqual(AuthorizationPolicies.RequireAdmin, listPolicy);
        Assert.AreEqual(AuthorizationPolicies.RequireAdmin, unlockPolicy);
        Assert.IsNull(lockMethod);
    }

    private static AuthController CreateController(IAuthService authService)
    {
        ClaimsPrincipal user = new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            AuthenticationDefaults.SchemeName));
        AuthController controller = new(authService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            },
        };

        return controller;
    }

    private sealed class StubAuthService : IAuthService
    {
        public IReadOnlyList<UserListItem> Accounts { get; init; } = [];

        public Guid? ListAccountsActorUserId { get; private set; }

        public Guid? UnlockedUserId { get; private set; }

        public Guid? UnlockActorUserId { get; private set; }

        public Exception? VerifyException { get; init; }

        public Guid? VerifiedUserId { get; private set; }

        public Task<AuthSession> LoginAsync(string username, string password, string? clientIp = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> LogoutAsync(string sessionToken, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<AuthSession> ValidateSessionAsync(string sessionToken, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task VerifyPasswordForUnlockAsync(Guid userId, string password, CancellationToken ct = default)
        {
            this.VerifiedUserId = userId;

            return this.VerifyException is null
                ? Task.CompletedTask
                : Task.FromException(this.VerifyException);
        }

        public Task<AuthSession> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string sessionToken, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IssuedCredential> CreateAccountAsync(string username, bool isAdmin, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task UnlockAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default)
        {
            this.UnlockedUserId = targetUserId;
            this.UnlockActorUserId = actorUserId;
            return Task.CompletedTask;
        }

        public Task DisableAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task EnableAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IssuedCredential> ResetPasswordAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UserListItem>> ListAccountsAsync(Guid actorUserId, CancellationToken ct = default)
        {
            this.ListAccountsActorUserId = actorUserId;
            return Task.FromResult(this.Accounts);
        }
    }
}