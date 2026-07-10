using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 持久化端口详细配置；Provider 选择由 <c>InkwellProvidersOptions.Persistence</c> 承载（F9），
/// 本类只承载连接 / 超时 / Seed 等详细配置。
/// </summary>
public sealed class PersistenceOptions
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>启动期是否自动执行 <c>InkwellSeeder.SeedAsync()</c>（ADR-021 D2）。</summary>
    public bool AutoSeedOnStartup { get; set; } = true;

    public bool EnableSensitiveDataLogging { get; set; }
}
