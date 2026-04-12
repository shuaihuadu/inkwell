using Inkwell;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell.Persistence.EntityFrameworkCore;

/// <summary>
/// 配置文件驱动的持久化扩展方法
/// 从 appsettings.json 中的 Persistence 节点读取配置
/// 
/// appsettings.json 配置示例：
/// <code>
/// {
///   "Persistence": {
///     "Provider": "InMemory",          // InMemory 或 SqlServer
///     "ConnectionString": "Server=...", // 仅 SqlServer 时需要
///     "DatabaseName": "InkwellDb"       // 仅 InMemory 时可选
///   }
/// }
/// </code>
/// 
/// 使用 SqlServer 时需要引用 Inkwell.Persistence.SqlServer 包并调用 UseSqlServer。
/// 此方法仅处理 InMemory 的自动配置。SqlServer 需显式调用 UseSqlServer() 或由宿主项目处理。
/// </summary>
public static class InkwellCoreBuilderConfigExtensions
{
    /// <summary>
    /// 根据配置文件自动选择持久化提供程序
    /// </summary>
    /// <param name="builder">Inkwell 核心构建器</param>
    /// <param name="configuration">应用配置</param>
    /// <param name="sqlServerConfigurator">SQL Server 配置委托（需引用 Inkwell.Persistence.SqlServer 才能提供）</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseConfiguredPersistence(
        this InkwellCoreBuilder builder,
        IConfiguration configuration,
        Action<InkwellCoreBuilder, string>? sqlServerConfigurator = null)
    {
        string provider = configuration["Persistence:Provider"] ?? "InMemory";

        switch (provider.ToLowerInvariant())
        {
            case "sqlserver":
                string connectionString = configuration["Persistence:ConnectionString"]
                    ?? throw new InvalidOperationException("Persistence:ConnectionString is required when using SqlServer provider.");

                if (sqlServerConfigurator is not null)
                {
                    sqlServerConfigurator(builder, connectionString);
                }
                else
                {
                    throw new InvalidOperationException(
                        "SqlServer provider requires a configurator. Add reference to Inkwell.Persistence.SqlServer and pass (b, cs) => b.UseSqlServer(cs).");
                }
                break;

            case "inmemory":
            default:
                string databaseName = configuration["Persistence:DatabaseName"] ?? "InkwellDb";

                builder.Services.AddDbContext<InkwellDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                });

                builder.Services.AddInkwellEfCorePersistence();
                break;
        }

        return builder;
    }
}
