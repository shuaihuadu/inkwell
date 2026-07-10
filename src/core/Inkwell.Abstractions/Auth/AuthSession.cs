// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>登录 / 会话校验成功后返回给调用方的会话信息 DTO。</summary>
public sealed record class AuthSession(Guid UserId, string Username, bool IsSuper, string SessionToken, DateTimeOffset ExpiresAt)
{
    public Guid UserId { get; init; } = UserId != Guid.Empty ? UserId : throw new ArgumentException("UserId must not be empty.", nameof(UserId));

    public string Username { get; init; } = !string.IsNullOrEmpty(Username) ? Username : throw new ArgumentException("Username must not be empty.", nameof(Username));

    public string SessionToken { get; init; } = !string.IsNullOrEmpty(SessionToken) ? SessionToken : throw new ArgumentException("SessionToken must not be empty.", nameof(SessionToken));
}
