// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class UserEntity : IHasTimestamps
{
    public Guid Id { get; init; }

    public string Username { get; init; } = "";

    public string PasswordHash { get; init; } = "";

    public bool IsSuper { get; init; }

    public bool IsLocked { get; init; }

    public int FailedUnlockAttempts { get; init; }

    public DateTimeOffset? LastLoginTime { get; init; }

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }

}
