// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 提供队列投递与死信处理配置。
/// </summary>
public sealed class QueueOptions
{
    /// <summary>
    /// 获取或设置消息最大投递次数。
    /// </summary>
    [Range(1, 100)]
    public int MaxDeliveryAttempts { get; set; } = 3;

    /// <summary>
    /// 获取或设置消息可见性超时时间（秒）。
    /// </summary>
    [Range(1, 3600)]
    public int VisibilityTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 获取或设置死信消息保留时间（小时）。
    /// </summary>
    [Range(1, 168)]
    public int DlqRetentionHours { get; set; } = 24;

    /// <summary>
    /// 获取或设置是否启用敏感数据日志记录。
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }
}
