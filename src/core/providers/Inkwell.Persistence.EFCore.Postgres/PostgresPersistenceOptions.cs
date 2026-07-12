// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell.Persistence.EFCore.Postgres;

/// <summary>Postgres 专属配置——连接重试参数。</summary>
public sealed class PostgresPersistenceOptions
{
    /// <summary>获取或设置最大连接重试次数。</summary>
    [Range(0, 20)]
    public int MaxRetryCount { get; set; } = 6;

    /// <summary>获取或设置连接重试的最大延迟秒数。</summary>
    [Range(1, 300)]
    public int MaxRetryDelaySeconds { get; set; } = 30;
}
