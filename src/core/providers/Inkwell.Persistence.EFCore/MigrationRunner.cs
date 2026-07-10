using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Inkwell;
using System.Diagnostics;

namespace Inkwell.Persistence.EFCore;

/// <summary>
/// <see cref="MigrateAsync"/> 仅负责 schema 初始化（Migrate / EnsureCreated）；<see cref="SeedAsync"/> 仅负责按
/// <see cref="PersistenceOptions.AutoSeedOnStartup"/> 开关执行 Seed。两方法调用顺序 / 是否调用由调用方决定。
/// </summary>
internal sealed class MigrationRunner(InkwellDbContext db, IDbContextInitializer initializer, IOptions<PersistenceOptions> options, InkwellSeeder seeder, ILogger<MigrationRunner> logger)
{
    public async Task MigrateAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Migration begin provider={ProviderName}", db.Database.ProviderName);
        Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(options.Value.CommandTimeoutSeconds));

        try
        {
            await initializer.InitializeAsync(db, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Migration timeout: {options.Value.CommandTimeoutSeconds}s");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Migration failed", ex);
        }

        sw.Stop();
        logger.LogInformation("Migration ok elapsed={Ms}ms", sw.ElapsedMilliseconds);
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!options.Value.AutoSeedOnStartup)
        {
            return;
        }

        logger.LogInformation("Seed begin");
        Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        await seeder.SeedAsync(ct).ConfigureAwait(false);

        sw.Stop();
        logger.LogInformation("Seed ok elapsed={Ms}ms", sw.ElapsedMilliseconds);
    }
}
