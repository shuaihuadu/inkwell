// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Postgres;

/// <summary>
/// 为 EF Core CLI 创建 Postgres 设计时数据库上下文。
/// </summary>
public sealed class PostgresDesignTimeDbContextFactory : IDesignTimeDbContextFactory<InkwellDbContext>
{
    /// <inheritdoc />
    public InkwellDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<InkwellDbContext> optionsBuilder = new();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=inkwell_design;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsAssembly("Inkwell.Persistence.EFCore.Postgres"))
            .UseSnakeCaseNamingConvention();

        return new InkwellDbContext(optionsBuilder.Options);
    }
}