using Inkwell.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inkwell.Persistence.SqlServer;

/// <summary>
/// 设计时 DbContext 工厂
/// 用于 EF Core 迁移命令（dotnet ef migrations add / update）
/// </summary>
public sealed class InkwellDbContextFactory : IDesignTimeDbContextFactory<InkwellDbContext>
{
    /// <inheritdoc />
    public InkwellDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<InkwellDbContext> optionsBuilder = new();

        // 迁移时使用的默认连接字符串（仅用于生成迁移代码，不会实际连接）
        string connectionString = args.Length > 0
            ? args[0]
            : "Server=(localdb)\\mssqllocaldb;Database=InkwellDb;Trusted_Connection=True;";

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(typeof(InkwellDbContextFactory).Assembly.FullName);
        });

        return new InkwellDbContext(optionsBuilder.Options);
    }
}
