namespace Inkwell.Persistence.EFCore.Postgres;

/// <summary>Postgres 场景下走 <see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/>（与 SqlServer 完全对称）。</summary>
internal sealed class PostgresDbContextInitializer : IDbContextInitializer
{
    public Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default) => db.Database.MigrateAsync(ct);
}
