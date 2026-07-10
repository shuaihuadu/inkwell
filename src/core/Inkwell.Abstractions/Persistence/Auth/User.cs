
namespace Inkwell;

/// <summary>账号业务 Model。</summary>
public sealed record class User : IHasTimestamps, IHasRowVersion
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }

    public required string PasswordHash { get; init; }

    public bool IsSuper { get; init; }

    public bool IsLocked { get; init; }

    public int FailedUnlockAttempts { get; init; }

    public DateTimeOffset? LastLoginTime { get; init; }

    public required DateTimeOffset CreatedTime { get; init; }

    public required DateTimeOffset UpdatedTime { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
