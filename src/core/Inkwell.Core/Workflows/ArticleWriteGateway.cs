using Inkwell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Inkwell.Workflows;

/// <summary>
/// 文章写入网关 —— 供 Workflow 内单例 Executor 在运行时安全调用 Scoped 持久化 Provider
/// </summary>
/// <remarks>
/// <para>
/// 工作流中的 Executor 是单例，不能直接捕获 Scoped 的 <see cref="IArticlePersistenceProvider"/>。
/// 本网关在每次 <see cref="AddAsync"/> 内部创建临时 Scope，解析 Provider 执行写入后释放 Scope，
/// 既保证 DbContext 生命周期正确，又让 Executor 层无需感知 DI Scope 管理。
/// </para>
/// <para>
/// <see cref="ScopeFactory"/> 在 DI 构建完成后由宿主赋值，赋值前调用 <see cref="AddAsync"/>
/// 会记录警告并直接跳过，避免启动早期误触发导致异常。
/// </para>
/// </remarks>
public sealed class ArticleWriteGateway
{
    /// <summary>
    /// 获取或设置 Scope 工厂。应在 <see cref="IHost"/> 构建完成后、首次 Workflow 运行前被赋值
    /// </summary>
    public IServiceScopeFactory? ScopeFactory { get; set; }

    /// <summary>
    /// 异步写入一条文章记录；网关未就绪时静默跳过
    /// </summary>
    /// <param name="record">待持久化的文章记录</param>
    /// <param name="logger">可选日志（为 null 时会从 Scope 内解析 <see cref="ILogger{ArticleWriteGateway}"/>）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddAsync(ArticleRecord record, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        IServiceScopeFactory? factory = this.ScopeFactory;
        if (factory is null)
        {
            (logger ?? NullLogger.Instance).LogWarning(
                "[ArticleWriteGateway] ScopeFactory not initialized, skipping persistence. Id={Id}", record.Id);
            return;
        }

        await using AsyncServiceScope scope = factory.CreateAsyncScope();

        // 外层没传 logger 时，从 Scope 里拿一个真实 logger，避免诊断信息被吞
        ILogger effectiveLogger = logger
            ?? scope.ServiceProvider.GetService<ILogger<ArticleWriteGateway>>()
            ?? (ILogger)NullLogger.Instance;

        IArticlePersistenceProvider? provider = scope.ServiceProvider.GetService<IArticlePersistenceProvider>();
        if (provider is null)
        {
            effectiveLogger.LogWarning(
                "[ArticleWriteGateway] IArticlePersistenceProvider not registered, skipping persistence. Id={Id}", record.Id);
            return;
        }

        try
        {
            await provider.AddAsync(record, cancellationToken).ConfigureAwait(false);
            effectiveLogger.LogInformation(
                "[ArticleWriteGateway] Article persisted. Id={Id} Title={Title}", record.Id, record.Title);
        }
        catch (Exception ex)
        {
            effectiveLogger.LogError(ex,
                "[ArticleWriteGateway] Failed to persist article. Id={Id} Title={Title}", record.Id, record.Title);
            throw;
        }
    }
}
