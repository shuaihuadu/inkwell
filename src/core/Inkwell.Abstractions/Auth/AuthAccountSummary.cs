namespace Inkwell;

/// <summary>账号列表投影 DTO；不含 <c>PasswordHash</c>（防御性设计）。</summary>
public sealed record class AuthAccountSummary(Guid UserId, string Username, bool IsSuper, bool IsLocked, DateTimeOffset? LastLoginTime, DateTimeOffset CreatedTime);
