using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Inkwell;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Inkwell.Persistence.EFCore.SqlServer.DependencyInjection;

HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder(args);

string providerName = hostBuilder.Configuration["Inkwell:Persistence:Provider"]
    ?? throw new InvalidOperationException("Missing configuration 'Inkwell:Persistence:Provider'. Expected 'SqlServer' or 'Postgres'.");
string connectionString = hostBuilder.Configuration.GetConnectionString("Inkwell")
    ?? throw new InvalidOperationException("Missing configuration 'ConnectionStrings:Inkwell'.");

IInkwellBuilder inkwellBuilder = hostBuilder.Services.AddInkwell(hostBuilder.Configuration);

_ = providerName switch
{
    "SqlServer" => inkwellBuilder.UseSqlServer(connectionString),
    "Postgres" => inkwellBuilder.UsePostgres(connectionString),
    _ => throw new InvalidOperationException($"Unknown Inkwell:Persistence:Provider '{providerName}'. Expected 'SqlServer' or 'Postgres'."),
};

// Inkwell.Migrator 只装配 IPersistenceProvider 这一个端口，不调用 IInkwellBuilder.Build()——
// Build() 会强制校验 FileStorage/Cache/Queue/AgentRuntime 全部端口均已注册（IInkwellBuilder.Build()
// 的 EnsureRequiredPortRegistered 系列检查），Migrator 不需要也不装配这四个端口。
using IHost host = hostBuilder.Build();
using IServiceScope scope = host.Services.CreateScope();

ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    MigrationRunner runner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();

    await runner.MigrateAsync().ConfigureAwait(false);
    await runner.SeedAsync().ConfigureAwait(false);

    logger.LogInformation("Inkwell.Migrator completed successfully provider={ProviderName}", providerName);

    return 0;
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Inkwell.Migrator failed provider={ProviderName}", providerName);

    return 1;
}
