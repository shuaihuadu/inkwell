// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

public sealed class AuthOptions
{
    [Range(1, 24)]
    public int SessionTtlHours { get; set; } = 24;

    [Range(1, 20)]
    public int MaxFailedUnlockAttempts { get; set; } = 5;

    public bool EnableSensitiveDataLogging { get; set; }
}
