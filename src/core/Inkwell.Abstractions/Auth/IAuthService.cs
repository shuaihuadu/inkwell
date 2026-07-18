// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>登录鉴权业务对外接口。</summary>
public interface IAuthService
{
    /// <summary>
    /// 使用用户名和密码登录。
    /// </summary>
    /// <param name="username">用户名。</param>
    /// <param name="password">密码。</param>
    /// <param name="clientIp">客户端 IP 地址。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>登录成功后的认证会话。</returns>
    Task<AuthSession> LoginAsync(string username, string password, string? clientIp = null, CancellationToken ct = default);

    /// <summary>幂等：<c>true</c> = 找到并已登出，<c>false</c> = token 未知或已失效。</summary>
    /// <param name="sessionToken">会话令牌。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>会话已登出时为 <see langword="true"/>；令牌未知或已失效时为 <see langword="false"/>。</returns>
    Task<bool> LogoutAsync(string sessionToken, CancellationToken ct = default);

    /// <summary>
    /// 验证会话令牌。
    /// </summary>
    /// <param name="sessionToken">会话令牌。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>有效令牌对应的认证会话。</returns>
    Task<AuthSession> ValidateSessionAsync(string sessionToken, CancellationToken ct = default);

    /// <summary>客户端自动锁定的密码再验证；失败计数累加，达阈值触发临时锁账号。</summary>
    /// <param name="userId">用户标识。</param>
    /// <param name="password">待验证的密码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task VerifyPasswordForUnlockAsync(Guid userId, string password, CancellationToken ct = default);

    /// <summary>修改当前用户密码，并保留当前会话。</summary>
    /// <param name="userId">当前用户标识。</param>
    /// <param name="currentPassword">当前密码。</param>
    /// <param name="newPassword">新密码。</param>
    /// <param name="sessionToken">当前会话令牌。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>更新后的认证会话。</returns>
    Task<AuthSession> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string sessionToken, CancellationToken ct = default);

    /// <summary>管理员创建账号并签发一次性临时密码。</summary>
    /// <param name="username">用户名。</param>
    /// <param name="isAdmin">是否为管理员。</param>
    /// <param name="actorUserId">执行操作的管理员用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>仅显示一次的临时凭据。</returns>
    Task<IssuedCredential> CreateAccountAsync(string username, bool isAdmin, Guid actorUserId, CancellationToken ct = default);

    /// <summary>管理员解封账号。</summary>
    /// <param name="targetUserId">目标用户标识。</param>
    /// <param name="actorUserId">执行操作的管理员用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task UnlockAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>管理员禁用账号。</summary>
    Task DisableAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>管理员启用账号。</summary>
    Task EnableAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>管理员重置账号密码并签发一次性临时密码。</summary>
    Task<IssuedCredential> ResetPasswordAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>
    /// 获取账号列表。
    /// </summary>
    /// <param name="actorUserId">执行查询的管理员用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>账号摘要列表。</returns>
    Task<IReadOnlyList<UserListItem>> ListAccountsAsync(Guid actorUserId, CancellationToken ct = default);
}
