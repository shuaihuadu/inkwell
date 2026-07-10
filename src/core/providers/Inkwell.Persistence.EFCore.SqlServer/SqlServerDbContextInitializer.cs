namespace Inkwell.Persistence.EFCore.SqlServer;

/// <summary>SqlServer 场景下走 <see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/>（应用全部待处理 Migration）。</summary>
internal sealed class SqlServerDbContextInitializer : IDbContextInitializer
{
    public Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default) => db.Database.MigrateAsync(ct);
}
