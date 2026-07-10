using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Inkwell;

namespace Inkwell.Persistence.EFCore;

/// <summary>
/// 唯一 <see cref="IPersistenceProvider"/> 实现（ADR-021 D1）。只负责事务边界；具名 Repository 由业务服务
/// 直接构造函数注入，与本类共享同一 Scoped <see cref="InkwellDbContext"/> 实例，因此自动参与同一事务。
/// </summary>
internal sealed class EfCorePersistenceProvider(InkwellDbContext db, ILogger<EfCorePersistenceProvider> logger) : IPersistenceProvider
{
    private static readonly ActivitySource ActivitySource = new("Inkwell.Persistence.EFCore");

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) =>
        this.ExecuteInTransactionAsync(async innerCt =>
        {
            await action(innerCt).ConfigureAwait(false);

            return true;
        }, ct);

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
    {
        IExecutionStrategy strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using Activity? activity = ActivitySource.StartActivity("db.transaction");
            Stopwatch sw = Stopwatch.StartNew();

            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            logger.LogDebug("Transaction begin {ScopeId}", transaction.TransactionId);

            try
            {
                TResult? result = await action(ct).ConfigureAwait(false);

                await transaction.CommitAsync(ct).ConfigureAwait(false);
                logger.LogInformation("Transaction committed {ScopeId} elapsed={ElapsedMs}ms", transaction.TransactionId, sw.ElapsedMilliseconds);

                return result;
            }
            catch (OperationCanceledException)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            catch (KeyNotFoundException)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            catch (ArgumentException)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                logger.LogWarning(ex, "Transaction rolled back {ScopeId} exceptionType={ExceptionType}", transaction.TransactionId, ex.GetType().FullName);
                activity?.SetStatus(ActivityStatusCode.Error);
                activity?.AddException(ex);
                throw new InvalidOperationException($"Transaction rolled back: {ex.Message}", ex);
            }
        }).ConfigureAwait(false);
    }
}
