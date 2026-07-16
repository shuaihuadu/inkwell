// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>账号业务 Model。</summary>
public sealed record class User : IHasTimestamps
{
    /// <summary>
    /// 获取用户标识。
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// 获取用户名。
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// 获取密码哈希。
    /// </summary>
    public required string PasswordHash { get; init; }

    /// <summary>
    /// 获取用户是否为超级管理员。
    /// </summary>
    public bool IsSuper { get; init; }

    /// <summary>
    /// 获取账号是否已锁定。
    /// </summary>
    public bool IsLocked { get; init; }

    /// <summary>
    /// 获取连续密码验证失败次数（登录与客户端解锁场景共用同一计数）。
    /// </summary>
    public int FailedUnlockAttempts { get; init; }

    /// <summary>
    /// 获取最近登录时间。
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; init; }

    /// <summary>
    /// 获取用户创建时间。
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>
    /// 获取用户更新时间。
    /// </summary>
    public required DateTimeOffset UpdatedTime { get; init; }

}
