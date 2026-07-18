// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Auth;
using Microsoft.AspNetCore.RateLimiting;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 提供用户登录会话 API。
/// </summary>
[Route("api/auth")]
[Authorize]
public sealed class AuthController(IAuthService authService) : InkwellControllerBase
{
    /// <summary>
    /// 使用用户名和密码登录。
    /// </summary>
    /// <param name="request">登录请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>登录成功后的认证会话。</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthorizationPolicies.AuthRateLimiterPolicy)]
    [ProducesResponseType<AuthSession>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<AuthSession>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            AuthSession session = await authService.LoginAsync(
                request.Username,
                request.Password,
                this.HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken).ConfigureAwait(false);

            return this.Ok(session);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Unauthorized();
        }
        catch (InvalidOperationException)
        {
            return this.StatusCode(StatusCodes.Status423Locked);
        }
    }

    /// <summary>
    /// 登出当前会话。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = await authService.LogoutAsync(this.GetRequiredBearerToken(), cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return this.Unauthorized();
        }
    }

    /// <summary>
    /// 获取当前认证会话。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前认证会话。</returns>
    [HttpGet("session")]
    [ProducesResponseType<AuthSession>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthSession>> GetSessionAsync(CancellationToken cancellationToken)
    {
        try
        {
            AuthSession session = await authService.ValidateSessionAsync(
                this.GetRequiredBearerToken(),
                cancellationToken).ConfigureAwait(false);

            return this.Ok(session);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Unauthorized();
        }
    }

    /// <summary>
    /// 验证当前用户密码以解除客户端锁定。
    /// </summary>
    /// <param name="request">解锁请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpPost("unlock")]
    [EnableRateLimiting(AuthorizationPolicies.AuthRateLimiterPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> UnlockAsync(
        UnlockRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await authService.VerifyPasswordForUnlockAsync(
                this.GetRequiredUserId(),
                request.Password,
                cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return this.Unauthorized();
        }
        catch (InvalidOperationException)
        {
            return this.StatusCode(StatusCodes.Status423Locked);
        }
    }

    /// <summary>
    /// 修改当前用户密码。
    /// </summary>
    [HttpPost("password")]
    [EnableRateLimiting(AuthorizationPolicies.AuthRateLimiterPolicy)]
    [ProducesResponseType<AuthSession>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSession>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            AuthSession session = await authService.ChangePasswordAsync(
                this.GetRequiredUserId(),
                request.CurrentPassword,
                request.NewPassword,
                this.GetRequiredBearerToken(),
                cancellationToken).ConfigureAwait(false);

            return this.Ok(session);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Unauthorized();
        }
    }

    /// <summary>
    /// 获取部署内的账号列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>账号摘要列表。</returns>
    [HttpGet("accounts")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType<IReadOnlyList<UserListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserListItem>>> ListAccountsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<UserListItem> accounts = await authService
            .ListAccountsAsync(this.GetRequiredUserId(), cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(accounts);
    }

    /// <summary>创建账号并返回仅显示一次的临时凭据。</summary>
    [HttpPost("accounts")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType<IssuedCredential>(StatusCodes.Status201Created)]
    public async Task<ActionResult<IssuedCredential>> CreateAccountAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        IssuedCredential credential = await authService.CreateAccountAsync(
            request.Username,
            request.IsAdmin,
            this.GetRequiredUserId(),
            cancellationToken).ConfigureAwait(false);

        return this.StatusCode(StatusCodes.Status201Created, credential);
    }

    /// <summary>
    /// 解封指定账号。
    /// </summary>
    /// <param name="userId">目标用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpPost("accounts/{userId:guid}/unlock")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        await authService
            .UnlockAccountAsync(userId, this.GetRequiredUserId(), cancellationToken)
            .ConfigureAwait(false);

        return this.NoContent();
    }

    /// <summary>禁用指定账号。</summary>
    [HttpPost("accounts/{userId:guid}/disable")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    public async Task<IActionResult> DisableAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        await authService.DisableAccountAsync(userId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }

    /// <summary>启用指定账号。</summary>
    [HttpPost("accounts/{userId:guid}/enable")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    public async Task<IActionResult> EnableAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        await authService.EnableAccountAsync(userId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }

    /// <summary>重置指定账号密码并返回仅显示一次的临时凭据。</summary>
    [HttpPost("accounts/{userId:guid}/reset-password")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType<IssuedCredential>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IssuedCredential>> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken)
    {
        IssuedCredential credential = await authService
            .ResetPasswordAsync(userId, this.GetRequiredUserId(), cancellationToken)
            .ConfigureAwait(false);
        return this.Ok(credential);
    }
}