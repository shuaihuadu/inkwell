// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 提供用户登录会话 API。
/// </summary>
[Route("api/auth")]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
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
        _ = await authService.LogoutAsync(this.GetRequiredBearerToken(), cancellationToken).ConfigureAwait(false);

        return this.NoContent();
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
        AuthSession session = await authService.ValidateSessionAsync(
            this.GetRequiredBearerToken(),
            cancellationToken).ConfigureAwait(false);

        return this.Ok(session);
    }

    /// <summary>
    /// 验证当前用户密码以解除客户端锁定。
    /// </summary>
    /// <param name="request">解锁请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpPost("unlock")]
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
}