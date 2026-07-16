// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell;
using Inkwell.Cache.InMemory;
using Inkwell.FileStorage.Local;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Inkwell.Queue.Channels;
using Inkwell.VectorStore.InMemory;
using Inkwell.WebApi;
using Inkwell.WebApi.Errors;
using Inkwell.WebApi.Protocols;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
AzureOpenAICredential azureOpenAICredential = builder.Configuration
    .GetSection("Inkwell:AzureOpenAI")
    .Get<AzureOpenAICredential>() ?? new AzureOpenAICredential();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("inkwell-webapi"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());
builder.Logging.AddOpenTelemetry(logging => logging
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("inkwell-webapi"))
    .AddOtlpExporter());

// ADR-019：Inkwell.WebApi 仅注册 IQueueProvider enqueue 侧，不跑 consumer（consumer 归 Inkwell.Worker）。
// dev 默认 Provider 组合：Postgres（ADR-005 docker-compose 默认）+ 进程内 Cache/Queue/FileStorage/VectorStore。
// prod 切换到 MinIO/AzureBlob/Redis/Qdrant 的 Use*() 扩展待对应 provider csproj 落地实现后接入。
IInkwellBuilder inkwellBuilder = builder.Services.AddInkwell(builder.Configuration)
    .UsePostgres(builder.Configuration.GetConnectionString("Inkwell") ?? throw new InvalidOperationException("Missing ConnectionStrings:Inkwell."))
    .UseInMemoryCache()
    .UseChannelsQueue()
    .UseLocalFileSystemFileStorage()
    .UseInMemoryVectorStore()
    .UseDefaultAuthService()
    .UseDefaultAgentServices()
    .UseDefaultConversationService()
    .UseDefaultToolService()
    .UseDefaultSkillService()
    .AddModelRegistry()
    .AddLiteLLMModelRegistrySource();

if (!string.IsNullOrWhiteSpace(azureOpenAICredential.Endpoint)
    && !string.IsNullOrWhiteSpace(azureOpenAICredential.ApiKey))
{
    inkwellBuilder.UseAzureOpenAIModelRuntime(azureOpenAICredential);
}

inkwellBuilder.Build();

builder.Services.AddSessionAuthentication();
builder.Services.AddAgentProtocols();
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// 登录 / 解锁按客户端 IP 限流，防止密码暴力枚举（AuthController 上的 [EnableRateLimiting] 引用此策略名）。
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(AuthorizationPolicies.AuthRateLimiterPolicy, limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/", (IHostEnvironment environment) => environment.IsDevelopment()
    ? Results.Redirect("/scalar/v1")
    : Results.Ok(new { name = "Inkwell WebApi", status = "running" }));
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();
app.MapAgentProtocols(AuthorizationPolicies.RequireAuthenticatedUser);

app.Run();
