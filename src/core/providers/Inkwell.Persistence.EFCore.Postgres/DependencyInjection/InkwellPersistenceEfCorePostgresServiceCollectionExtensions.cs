// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.DependencyInjection;
using Inkwell.Persistence.EFCore.Postgres.Interceptors;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inkwell.Persistence.EFCore.Postgres.DependencyInjection;

/// <summary><see cref="IInkwellBuilder"/> 的 Postgres Provider 唯一入口扩展。</summary>
public static class InkwellPersistenceEfCorePostgresServiceCollectionExtensions
{
    public static IInkwellBuilder UsePostgres(this IInkwellBuilder builder, string connectionString, Action<PersistenceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.Services.AddEfCorePersistenceBase();

        builder.Services.AddOptions<PostgresPersistenceOptions>()
            .BindConfiguration("Inkwell:Persistence:Postgres")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 连接字符串单一来源：与 SqlServer 侧同款约定，避免两处配置键漂移。
        builder.Services.Configure<PersistenceOptions>(o => o.ConnectionString = connectionString);

        if (configure is not null)
        {
            builder.Services.PostConfigure(configure);
        }

        // Owner picker（2026-07-06）：Postgres 走手动 RowVersion 模拟，不用原生 xmin（详 HD-012 §4）。
        builder.Services.AddSingleton<ISaveChangesInterceptor, PostgresRowVersionInterceptor>();

        builder.Services.AddDbContext<InkwellDbContext>((sp, options) =>
        {
            PersistenceOptions persistenceOptions = sp.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            PostgresPersistenceOptions postgresOptions = sp.GetRequiredService<IOptions<PostgresPersistenceOptions>>().Value;

            options
                .UseNpgsql(connectionString, npgsql => npgsql
                    .MigrationsAssembly("Inkwell.Persistence.EFCore.Postgres")
                    .EnableRetryOnFailure(
                        postgresOptions.MaxRetryCount,
                        TimeSpan.FromSeconds(postgresOptions.MaxRetryDelaySeconds),
                        errorCodesToAdd: null)
                    .CommandTimeout(persistenceOptions.CommandTimeoutSeconds))
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });

        builder.Services.AddSingleton<IDbContextInitializer, PostgresDbContextInitializer>();

        return builder;
    }
}
