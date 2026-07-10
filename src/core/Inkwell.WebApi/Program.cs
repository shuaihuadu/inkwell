using Microsoft.Extensions.Options;
using Inkwell;
using Inkwell.Cache.InMemory;
using Inkwell.Queue.Channels;
using Inkwell.FileStorage.Local;
using Inkwell.VectorStore.InMemory;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Inkwell.Persistence.EFCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ADR-019：Inkwell.WebApi 仅注册 IQueueProvider enqueue 侧，不跑 consumer（consumer 归 Inkwell.Worker）。
// dev 默认 Provider 组合：Postgres（ADR-005 docker-compose 默认）+ 进程内 Cache/Queue/FileStorage/VectorStore。
// prod 切换到 MinIO/AzureBlob/Redis/Qdrant 的 Use*() 扩展待对应 provider csproj 落地实现后接入。
builder.Services.AddInkwell(builder.Configuration)
    .UsePostgres(builder.Configuration.GetConnectionString("Inkwell") ?? throw new InvalidOperationException("Missing ConnectionStrings:Inkwell."))
    .UseInMemoryCache()
    .UseChannelsQueue()
    .UseLocalFileSystemFileStorage()
    .UseInMemoryVectorStore()
    .UseAzureOpenAIAgentRuntime()
    .UseDefaultAuthService()
    .UseDefaultAgentService()
    .UseDefaultToolService()
    .UseDefaultConversationService()
    .UseDefaultSkillService()
    .AddDefaultModelCatalog()
    .Build();

builder.Services.AddOpenApi();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Migration 由 CI/CD 独立步骤执行（ADR-021 2026-07-06 errata），启动期只跑 Seed。
using (IServiceScope scope = app.Services.CreateScope())
{
    MigrationRunner migrationRunner = scope.ServiceProvider.GetRequiredService<Inkwell.Persistence.EFCore.MigrationRunner>();

    await migrationRunner.SeedAsync();
}

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

// REST CRUD / AG-UI SSE / Public API 端点：Inkwell.WebApi 尚无独立 HD，端点设计留待后续任务补齐。

app.Run();
