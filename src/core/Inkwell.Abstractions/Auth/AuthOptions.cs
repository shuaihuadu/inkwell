// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 提供身份认证与会话管理配置。
/// </summary>
public sealed class AuthOptions
{
    /// <summary>
    /// 获取或设置会话有效期（小时）。
    /// </summary>
    [Range(1, 24)]
    public int SessionTtlHours { get; set; } = 24;

    /// <summary>
    /// 获取或设置解锁密码验证失败的最大次数。
    /// </summary>
    [Range(1, 20)]
    public int MaxFailedUnlockAttempts { get; set; } = 5;

    /// <summary>
    /// 获取或设置是否启用敏感数据日志记录。
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }
}
