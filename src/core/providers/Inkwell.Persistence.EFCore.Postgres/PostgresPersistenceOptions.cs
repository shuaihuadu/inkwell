using System.ComponentModel.DataAnnotations;

namespace Inkwell.Persistence.EFCore.Postgres;

/// <summary>Postgres 专属配置——连接重试参数。</summary>
public sealed class PostgresPersistenceOptions
{
    [Range(0, 20)]
    public int MaxRetryCount { get; set; } = 6;

    [Range(1, 300)]
    public int MaxRetryDelaySeconds { get; set; } = 30;
}
