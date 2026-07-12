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

    /// <summary>管理员解封账号。</summary>
    /// <param name="targetUserId">目标用户标识。</param>
    /// <param name="actorUserId">执行操作的管理员用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task UnlockAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>
    /// 获取账号列表。
    /// </summary>
    /// <param name="isLocked">锁定状态筛选条件；未指定时返回全部账号。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>账号摘要列表。</returns>
    Task<IReadOnlyList<AuthAccountSummary>> ListAccountsAsync(bool? isLocked, CancellationToken ct = default);
}
