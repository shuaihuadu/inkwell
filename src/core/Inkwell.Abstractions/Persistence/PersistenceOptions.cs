// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 持久化端口详细配置；Provider 选择由 <c>InkwellProvidersOptions.Persistence</c> 承载（F9），
/// 本类只承载连接 / 超时 / Seed 等详细配置。
/// </summary>
public sealed class PersistenceOptions
{
    /// <summary>
    /// 获取或设置数据库连接字符串。
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置数据库命令超时时间（秒）。
    /// </summary>
    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>启动期是否自动执行 <c>InkwellSeeder.SeedAsync()</c>（ADR-021 D2）。</summary>
    public bool AutoSeedOnStartup { get; set; } = true;

    /// <summary>
    /// 获取或设置持久化 Seed 配置。
    /// </summary>
    public PersistenceSeedOptions Seed { get; set; } = new();

    /// <summary>
    /// 获取或设置是否启用敏感数据日志记录。
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }
}
