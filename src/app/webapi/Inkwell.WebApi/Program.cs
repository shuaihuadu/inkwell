using Inkwell;
using Inkwell.Agents;
using Inkwell.Persistence.InMemory;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Inkwell.WebApi;

/// <summary>
/// Inkwell Web API 入口
/// </summary>
public static class Program
{
    /// <summary>
    /// OpenTelemetry 服务名称
    /// </summary>
    private const string ServiceName = "Inkwell.WebApi";

    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // 注册 Controller
        builder.Services.AddControllers();

        // 注册 Inkwell 核心服务 + 持久化 + Azure OpenAI 多模型
        builder.Services.AddInkwellCore()
            .UseInMemoryDatabase()
            .UseAzureOpenAI(builder.Configuration);

        // 注册所有 Agent（使用 Keyed IChatClient）
        AgentRegistry agentRegistry = builder.Services.AddInkwellAgents(builder.Configuration);

        // 配置 OpenTelemetry
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Microsoft.Agents.AI.Workflows*")
                    .AddSource(ServiceName)
                    .AddOtlpExporter();
            });

        // 配置 CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:5188", "http://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        WebApplication app = builder.Build();

        app.UseCors();
        app.MapControllers();

        // ========== 为每个 Agent 映射 AG-UI 端点 ==========
        foreach (AgentRegistration registration in agentRegistry.GetAll())
        {
            app.MapAGUI(registration.AguiRoute, registration.Agent);
        }

        app.Run();
    }
}
