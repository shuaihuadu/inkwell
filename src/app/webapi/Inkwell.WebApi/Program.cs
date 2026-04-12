using Inkwell;
using Inkwell.Agents;
using Inkwell.Persistence.InMemory;
using Inkwell.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Workflows;
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

        // 加载声明式 Agent（YAML 定义）
        IChatClient defaultClient = builder.Services
            .Where(d => d.ServiceType == typeof(IChatClient) && !d.IsKeyedService && d.ImplementationInstance is IChatClient)
            .Select(d => (IChatClient)d.ImplementationInstance!)
            .FirstOrDefault()!;

        string agentsDir = Path.Combine(builder.Environment.ContentRootPath, "Agents");
        DeclarativeAgentLoader.LoadFromDirectory(agentRegistry, defaultClient, agentsDir);

        // 注册 CMS MCP 工具服务
        builder.Services.AddSingleton<CmsMcpTools>();

        // 注册所有 Workflow
        WorkflowRegistry workflowRegistry = builder.Services.AddInkwellWorkflows();

        // 加载声明式 Workflow（YAML 定义）
        string workflowsDir = Path.Combine(builder.Environment.ContentRootPath, "Workflows");
        DeclarativeWorkflowLoader.LoadFromDirectory(workflowRegistry, defaultClient, workflowsDir);

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

        // ========== 将 Workflow 包装为 Agent 并映射 AG-UI 端点 ==========
        foreach (WorkflowRegistration workflowReg in workflowRegistry.GetAll())
        {
            // 仅对支持对话交互的 Workflow 暴露 AG-UI 端点
            string agentId = $"workflow-{workflowReg.Id}";

            // Workflow.AsAIAgent() 将 Workflow 转换为可对话的 Agent
            AIAgent workflowAgent = workflowReg.Workflow.AsAIAgent(agentId, workflowReg.Name);

            string aguiRoute = $"/api/agui/{agentId}";
            app.MapAGUI(aguiRoute, workflowAgent);

            // 同时注册到 AgentRegistry 以便前端发现
            agentRegistry.Register(new AgentRegistration
            {
                Id = agentId,
                Name = $"[Workflow] {workflowReg.Name}",
                Description = workflowReg.Description,
                Agent = workflowAgent,
                AguiRoute = aguiRoute
            });
        }

        app.Run();
    }
}
