using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Inkwell;
using Inkwell.Persistence.EFCore.DependencyInjection;

namespace Inkwell.Persistence.EFCore.SqlServer.DependencyInjection;

/// <summary><see cref="IInkwellBuilder"/> 的 SqlServer Provider 唯一入口扩展。</summary>
public static class InkwellPersistenceEfCoreSqlServerServiceCollectionExtensions
{
    public static IInkwellBuilder UseSqlServer(this IInkwellBuilder builder, string connectionString, Action<PersistenceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.Services.AddEfCorePersistenceBase();

        builder.Services.AddOptions<SqlServerPersistenceOptions>()
            .BindConfiguration("Inkwell:Persistence:SqlServer")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 连接字符串单一来源：UseSqlServer(connectionString) 同时透传给 DbContextOptionsBuilder 与
        // PersistenceOptions.ConnectionString，避免出现两处独立配置键相互漂移。
        builder.Services.Configure<PersistenceOptions>(o => o.ConnectionString = connectionString);

        if (configure is not null)
        {
            builder.Services.PostConfigure(configure);
        }

        builder.Services.AddDbContext<InkwellDbContext>((sp, options) =>
        {
            PersistenceOptions persistenceOptions = sp.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            SqlServerPersistenceOptions sqlServerOptions = sp.GetRequiredService<IOptions<SqlServerPersistenceOptions>>().Value;

            options
                .UseSqlServer(connectionString, sql => sql
                    .MigrationsAssembly("Inkwell.Persistence.EFCore.SqlServer")
                    .EnableRetryOnFailure(
                        sqlServerOptions.MaxRetryCount,
                        TimeSpan.FromSeconds(sqlServerOptions.MaxRetryDelaySeconds),
                        errorNumbersToAdd: null)
                    .CommandTimeout(persistenceOptions.CommandTimeoutSeconds))
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });

        builder.Services.AddSingleton<IDbContextInitializer, SqlServerDbContextInitializer>();

        return builder;
    }
}
