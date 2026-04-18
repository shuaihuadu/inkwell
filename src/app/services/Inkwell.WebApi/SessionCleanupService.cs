using Inkwell.Agents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inkwell.WebApi;

/// <summary>
/// 会话清理配置选项
/// </summary>
public sealed class SessionCleanupOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "SessionCleanup";

    /// <summary>
    /// 获取或设置清理间隔（小时），默认 24
    /// </summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>
    /// 获取或设置会话保留天数，默认 30
    /// </summary>
    public int RetentionDays { get; set; } = 30;
}

/// <summary>
/// 过期会话清理后台服务
/// 定期清理超过指定天数的历史会话
/// </summary>
public sealed class SessionCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<SessionCleanupOptions> options,
    ILogger<SessionCleanupService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SessionCleanupOptions config = options.Value;
        TimeSpan interval = TimeSpan.FromHours(config.IntervalHours);

        logger.LogInformation("[SessionCleanup] Started. Interval: {Interval}h, Retention: {Days} days",
            config.IntervalHours, config.RetentionDays);

        // 启动后先跑一次，避免频繁重启场景下永远不清理
        try
        {
            await CleanupExpiredSessionsAsync(config.RetentionDays, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SessionCleanup] Initial cleanup failed");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
                await CleanupExpiredSessionsAsync(config.RetentionDays, stoppingToken).ConfigureAwait(false);
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

    private async Task CleanupExpiredSessionsAsync(int retentionDays, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ISessionPersistenceProvider provider = scope.ServiceProvider.GetRequiredService<ISessionPersistenceProvider>();

        AgentRegistry registry = scope.ServiceProvider.GetRequiredService<AgentRegistry>();
        DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
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
                deletedCount, retentionDays);
        }
    }
}
