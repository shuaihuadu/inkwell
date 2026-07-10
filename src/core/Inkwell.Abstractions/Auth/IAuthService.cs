namespace Inkwell;

/// <summary>登录鉴权业务对外接口。</summary>
public interface IAuthService
{
    Task<AuthSession> LoginAsync(string username, string password, string? clientIp = null, CancellationToken ct = default);

    /// <summary>幂等：<c>true</c> = 找到并已登出，<c>false</c> = token 未知或已失效。</summary>
    Task<bool> LogoutAsync(string sessionToken, CancellationToken ct = default);

    Task<AuthSession> ValidateSessionAsync(string sessionToken, CancellationToken ct = default);

    /// <summary>客户端自动锁定的密码再验证；失败计数累加，达阈值触发临时锁账号。</summary>
    Task VerifyPasswordForUnlockAsync(Guid userId, string password, CancellationToken ct = default);

    /// <summary>管理员解封账号。</summary>
    Task UnlockAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default);

    Task<IReadOnlyList<AuthAccountSummary>> ListAccountsAsync(bool? isLocked, CancellationToken ct = default);
}
