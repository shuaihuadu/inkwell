// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

public sealed class CacheOptions
{
    [Range(1, 86400)]
    public int MinTtlSeconds { get; set; } = 1;

    [Range(1, 86400)]
    public int MaxTtlSeconds { get; set; } = 86400;

    [Range(1, 300)]
    public int DefaultLockTtlSeconds { get; set; } = 30;

    public bool EnableSensitiveDataLogging { get; set; }
}
