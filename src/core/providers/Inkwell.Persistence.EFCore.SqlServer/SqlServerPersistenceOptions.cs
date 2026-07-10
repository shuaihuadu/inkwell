// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell.Persistence.EFCore.SqlServer;

/// <summary>SqlServer 专属配置——连接重试参数。</summary>
public sealed class SqlServerPersistenceOptions
{
    [Range(0, 20)]
    public int MaxRetryCount { get; init; } = 6;

    [Range(1, 300)]
    public int MaxRetryDelaySeconds { get; init; } = 30;
}
