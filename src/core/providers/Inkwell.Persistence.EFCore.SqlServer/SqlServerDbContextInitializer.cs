// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.SqlServer;

/// <summary>SqlServer 场景下走 <see cref="M:Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade,System.Threading.CancellationToken)"/>（应用全部待处理 Migration）。</summary>
internal sealed class SqlServerDbContextInitializer : IDbContextInitializer
{
    public Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default) => db.Database.MigrateAsync(ct);
}
