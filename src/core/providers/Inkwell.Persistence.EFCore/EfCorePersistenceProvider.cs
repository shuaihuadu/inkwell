// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inkwell.Persistence.EFCore;

/// <summary>
/// 唯一 <see cref="IPersistenceProvider"/> 实现（ADR-021 D1）。
/// </summary>
internal sealed class EfCorePersistenceProvider(
    InkwellDbContext db,
    IServiceProvider services,
    ILogger<EfCorePersistenceProvider> logger) : IPersistenceProvider
{
    private static readonly ActivitySource activitySource = new("Inkwell.Persistence.EFCore");

    public TRepository GetRepository<TRepository>() where TRepository : notnull =>
        services.GetRequiredService<TRepository>();

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) =>
        this.ExecuteInTransactionAsync(IsolationLevel.Unspecified, async innerCt =>
        {
            await action(innerCt).ConfigureAwait(false);

            return true;
        }, ct);

    public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
        this.ExecuteInTransactionAsync(IsolationLevel.Unspecified, action, ct);

    public Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct = default) =>
        this.ExecuteInTransactionAsync(isolationLevel, async innerCt =>
        {
            await action(innerCt).ConfigureAwait(false);

            return true;
        }, ct);

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
    {
        IExecutionStrategy strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using Activity? activity = activitySource.StartActivity("db.transaction");
            Stopwatch sw = Stopwatch.StartNew();

            await using IDbContextTransaction transaction = isolationLevel == IsolationLevel.Unspecified
                ? await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false)
                : await db.Database.BeginTransactionAsync(isolationLevel, ct).ConfigureAwait(false);
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
