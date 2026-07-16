// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inkwell.Persistence.EFCore.Postgres.DependencyInjection;

/// <summary><see cref="IInkwellBuilder"/> 的 Postgres Provider 唯一入口扩展。</summary>
public static class InkwellPersistenceEfCorePostgresServiceCollectionExtensions
{
    /// <summary>注册使用 Postgres 的 EF Core 持久化服务。</summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <param name="connectionString">Postgres 连接字符串。</param>
    /// <param name="configure">可选的 <see cref="PersistenceOptions"/> 编程式追加配置。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
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
                .UseSnakeCaseNamingConvention()
                .ReplaceService<IModelCustomizer, PostgresModelCustomizer>();
        });

        builder.Services.AddSingleton<IDbContextInitializer, PostgresDbContextInitializer>();

        return builder;
    }
}
