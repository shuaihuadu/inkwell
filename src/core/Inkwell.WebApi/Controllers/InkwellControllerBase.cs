// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Claims;
using Microsoft.Extensions.Primitives;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 提供 Inkwell 业务 API 控制器共享的认证上下文读取能力。
/// </summary>
[ApiController]
public abstract class InkwellControllerBase : ControllerBase
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>
    /// 获取当前已认证用户标识。
    /// </summary>
    /// <returns>当前用户标识。</returns>
    protected Guid GetRequiredUserId()
    {
        string? value = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out Guid userId)
            ? userId
            : throw new UnauthorizedAccessException("Authenticated user identifier is missing or invalid.");
    }

    /// <summary>
    /// 获取当前请求携带的 Bearer 会话令牌。
    /// </summary>
    /// <returns>Bearer 会话令牌。</returns>
    protected string GetRequiredBearerToken()
    {
        if (!this.Request.Headers.TryGetValue("Authorization", out StringValues authorizationHeader))
        {
            throw new UnauthorizedAccessException("Missing bearer token.");
        }

        string headerValue = authorizationHeader.ToString();

        if (!headerValue.StartsWith(BearerPrefix, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid authorization scheme.");
        }

        string sessionToken = headerValue[BearerPrefix.Length..].Trim();

        return !string.IsNullOrEmpty(sessionToken)
            ? sessionToken
            : throw new UnauthorizedAccessException("Missing bearer token.");
    }
}