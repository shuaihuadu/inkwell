using Inkwell;
using Inkwell.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell.Persistence.SqlServer;

/// <summary>
/// SQL Server 持久化扩展方法
/// </summary>
public static class InkwellCoreBuilderSqlServerExtensions
{
    /// <summary>
    /// 使用 SQL Server 作为持久化存储
    /// </summary>
    /// <param name="builder">Inkwell 核心构建器</param>
    /// <param name="connectionString">SQL Server 连接字符串</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseSqlServer(this InkwellCoreBuilder builder, string connectionString)
    {
        builder.Services.AddDbContext<InkwellDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(InkwellCoreBuilderSqlServerExtensions).Assembly.FullName);
            });
        });

        builder.Services.AddInkwellEfCorePersistence();

        return builder;
    }
}
