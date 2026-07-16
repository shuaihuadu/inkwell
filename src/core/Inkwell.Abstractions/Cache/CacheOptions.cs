// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 提供缓存行为配置。
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// 获取或设置缓存项最短生存时间（秒）。
    /// </summary>
    [Range(1, 86400)]
    public int MinTtlSeconds { get; set; } = 1;

    /// <summary>
    /// 获取或设置缓存项最长生存时间（秒）。
    /// </summary>
    [Range(1, 86400)]
    public int MaxTtlSeconds { get; set; } = 86400;

    /// <summary>
    /// 获取或设置分布式锁的默认生存时间（秒）。
    /// </summary>
    [Range(1, 300)]
    public int DefaultLockTtlSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置是否启用敏感数据日志记录。
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }
}
