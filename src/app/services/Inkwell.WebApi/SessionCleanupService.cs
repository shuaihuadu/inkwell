using Inkwell.Agents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inkwell.WebApi;

/// <summary>
/// 过期会话清理后台服务
/// 定期清理超过指定天数的历史会话
/// </summary>
public sealed class SessionCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<SessionCleanupService> logger) : BackgroundService
{
    /// <summary>
    /// 清理间隔（默认每 24 小时执行一次）
    /// </summary>
    private static readonly TimeSpan s_interval = TimeSpan.FromHours(24);

    /// <summary>
    /// 会话保留天数（默认 30 天）
    /// </summary>
    private const int RetentionDays = 30;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[SessionCleanup] Started. Interval: {Interval}, Retention: {Days} days",
            s_interval, RetentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(s_interval, stoppingToken).ConfigureAwait(false);
                await CleanupExpiredSessionsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SessionCleanup] Error during cleanup");
            }
        }
    }

    private async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ISessionPersistenceProvider provider = scope.ServiceProvider.GetRequiredService<ISessionPersistenceProvider>();

        // 获取所有 agents 的会话（通过 agentId 遍历已知 Agent）
        AgentRegistry registry = scope.ServiceProvider.GetRequiredService<AgentRegistry>();
        DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddDays(-RetentionDays);
        int deletedCount = 0;

        foreach (AgentRegistration agent in registry.GetAll())
        {
            IReadOnlyList<SessionInfo> sessions = await provider
                .ListSessionInfosAsync(agent.Id, cancellationToken).ConfigureAwait(false);

            foreach (SessionInfo session in sessions)
            {
                if (session.UpdatedAt < cutoff)
                {
                    await provider.DeleteSessionAsync(session.Id, cancellationToken).ConfigureAwait(false);
                    deletedCount++;
                }
            }
        }

        if (deletedCount > 0)
        {
            logger.LogInformation("[SessionCleanup] Deleted {Count} expired sessions (older than {Days} days)",
                deletedCount, RetentionDays);
        }
    }
}
