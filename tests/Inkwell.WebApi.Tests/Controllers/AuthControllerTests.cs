// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Claims;
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

        public Task UnlockAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UserListItem>> ListAccountsAsync(bool? isLocked, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}