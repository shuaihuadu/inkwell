// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell;
using Inkwell.Cache.InMemory;
using Inkwell.FileStorage.Local;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Inkwell.Queue.Channels;
using Inkwell.VectorStore.InMemory;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("inkwell-worker"))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());
builder.Logging.AddOpenTelemetry(logging => logging
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("inkwell-worker"))
    .AddOtlpExporter());

// ADR-019：Inkwell.Worker 是后台进程（RedisStream consumer 侧 + DurableTask runner），不开 HTTP 业务端口。
// dev 默认 Provider 组合与 Inkwell.WebApi 对称；prod 切换到 MinIO/AzureBlob/Redis/Qdrant 待对应 provider csproj 落地后接入。
IInkwellBuilder inkwellBuilder = builder.Services.AddInkwell(builder.Configuration)
    .UsePostgres(builder.Configuration.GetConnectionString("Inkwell") ?? throw new InvalidOperationException("Missing ConnectionStrings:Inkwell."))
    .UseInMemoryCache()
    .UseChannelsQueue()
    .UseLocalFileSystemFileStorage()
    .UseInMemoryVectorStore()
    .UseDefaultAuthService()
    //.UseDefaultAgentService()
    .UseDefaultToolService()
    //.UseDefaultSessionService()
    .UseDefaultSkillService()
    .UseDefaultAgentRuntime()
    .UseLiteLLM();

inkwellBuilder.Build();

// KB ingest / Trigger fan-out 等消费侧 BackgroundService 尚无独立 HD，留待后续任务补齐；
// RedisStreamQueueProvider consumer group worker 与 DurableTask actor runner 同理。

IHost host = builder.Build();

// Worker 不跑 Migration 或 Seed；两者只由 Inkwell.Migrator 一次性进程执行（ADR-024）。

host.Run();
