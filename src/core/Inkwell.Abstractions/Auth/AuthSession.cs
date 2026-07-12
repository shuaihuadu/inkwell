// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>登录 / 会话校验成功后返回给调用方的会话信息 DTO。</summary>
/// <param name="UserId">用户标识。</param>
/// <param name="Username">用户名。</param>
/// <param name="IsSuper">是否为超级管理员。</param>
/// <param name="SessionToken">会话令牌。</param>
/// <param name="ExpiresAt">会话过期时间。</param>
public sealed record class AuthSession(Guid UserId, string Username, bool IsSuper, string SessionToken, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// 获取用户标识。
    /// </summary>
    public Guid UserId { get; init; } = UserId != Guid.Empty ? UserId : throw new ArgumentException("UserId must not be empty.", nameof(UserId));

    /// <summary>
    /// 获取用户名。
    /// </summary>
    public string Username { get; init; } = !string.IsNullOrEmpty(Username) ? Username : throw new ArgumentException("Username must not be empty.", nameof(Username));

    /// <summary>
    /// 获取会话令牌。
    /// </summary>
    public string SessionToken { get; init; } = !string.IsNullOrEmpty(SessionToken) ? SessionToken : throw new ArgumentException("SessionToken must not be empty.", nameof(SessionToken));
}
