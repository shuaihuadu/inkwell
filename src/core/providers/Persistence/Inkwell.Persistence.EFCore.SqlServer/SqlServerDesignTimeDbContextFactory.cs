// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.SqlServer;

/// <summary>
/// 为 EF Core CLI 创建 SQL Server 设计时数据库上下文。
/// </summary>
public sealed class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<InkwellDbContext>
{
    /// <inheritdoc />
    public InkwellDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<InkwellDbContext> optionsBuilder = new();

        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=inkwell_design;User ID=sa;Password=Inkwell-Local-2025!;TrustServerCertificate=True",
            sqlServer => sqlServer.MigrationsAssembly("Inkwell.Persistence.EFCore.SqlServer"))
            .ReplaceService<IModelCustomizer, SqlServerModelCustomizer>();

        return new InkwellDbContext(optionsBuilder.Options);
    }
}