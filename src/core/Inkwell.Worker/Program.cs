// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell;
using Inkwell.Cache.InMemory;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Inkwell.Queue.Channels;
using Inkwell.VectorStore.InMemory;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// ADR-019：Inkwell.Worker 是后台进程（RedisStream consumer 侧 + DurableTask runner），不开 HTTP 业务端口。
// dev 默认 Provider 组合与 Inkwell.WebApi 对称；prod 切换到 MinIO/AzureBlob/Redis/Qdrant 待对应 provider csproj 落地后接入。
builder.Services.AddInkwell(builder.Configuration)
    .UsePostgres(builder.Configuration.GetConnectionString("Inkwell") ?? throw new InvalidOperationException("Missing ConnectionStrings:Inkwell."))
    .UseInMemoryCache()
    .UseChannelsQueue()
    //.UseLocalFileSystemFileStorage()
    .UseInMemoryVectorStore()
    //.UseAzureOpenAIAgentRuntime()
    .UseDefaultAuthService()
    //.UseDefaultAgentService()
    .UseDefaultToolService()
    //.UseDefaultSessionService()
    .UseDefaultSkillService()
    .AddDefaultModelCatalog()
    .Build();

// KB ingest / Trigger fan-out 等消费侧 BackgroundService 尚无独立 HD，留待后续任务补齐；
// RedisStreamQueueProvider consumer group worker 与 DurableTask actor runner 同理。

IHost host = builder.Build();

// Worker 不跑 Migration（避免与 WebApi 双跑冲突，ADR-021），也不跑 Seed（仅 WebApi 启动时跑一次）。

host.Run();
