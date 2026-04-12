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

        // [C1 修复] 从 Keyed DI 中安全获取 Primary IChatClient
        IChatClient? primaryClient = null;
        foreach (ServiceDescriptor descriptor in builder.Services)
        {
            if (descriptor.ServiceType == typeof(IChatClient)
                && descriptor.IsKeyedService
                && descriptor.ServiceKey is string key
                && key == ModelServiceKeys.Primary
                && descriptor.KeyedImplementationInstance is IChatClient client)
            {
                primaryClient = client;
                break;
            }
        }

        if (primaryClient is null)
        {
            throw new InvalidOperationException("Primary IChatClient not registered. Ensure UseAzureOpenAI() is called before this point.");
        }

        // 注册所有 Agent（使用 Keyed IChatClient）
        AgentRegistry agentRegistry = builder.Services.AddInkwellAgents(builder.Configuration);

        // 加载声明式 Agent（YAML 定义）[M8 修复: 增加日志]
        ILogger<AgentRegistry> agentLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<AgentRegistry>();
        string agentsDir = Path.Combine(builder.Environment.ContentRootPath, "Agents");
        int loadedAgents = DeclarativeAgentLoader.LoadFromDirectory(agentRegistry, primaryClient, agentsDir, agentLogger);
        agentLogger.LogInformation("Loaded {Count} declarative agents from {Directory}", loadedAgents, agentsDir);

        // 注册 CMS MCP 工具服务
        builder.Services.AddSingleton<CmsMcpTools>();

        // 注册认证与授权服务
        builder.Services.AddInkwellAuth(builder.Configuration);

        // 注册所有 Workflow
        WorkflowRegistry workflowRegistry = builder.Services.AddInkwellWorkflows();

        // 加载声明式 Workflow（YAML 定义）
        string workflowsDir = Path.Combine(builder.Environment.ContentRootPath, "Workflows");
        int loadedWorkflows = DeclarativeWorkflowLoader.LoadFromDirectory(workflowRegistry, primaryClient, workflowsDir, agentLogger);
        agentLogger.LogInformation("Loaded {Count} declarative workflows from {Directory}", loadedWorkflows, workflowsDir);

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

        // [H7 修复] CORS origin 从配置读取
        string[] corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? ["http://localhost:5188", "http://localhost:3000"];

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        WebApplication app = builder.Build();

        // [L1 修复] 全局异常处理
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error");
        }

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // ========== 为每个 Agent 映射 AG-UI 端点 ==========
        foreach (AgentRegistration registration in agentRegistry.GetAll())
        {
            app.MapAGUI(registration.AguiRoute, registration.Agent);
        }

        // ========== 将 Workflow 包装为 Agent 并映射 AG-UI 端点 ==========
        foreach (WorkflowRegistration workflowReg in workflowRegistry.GetAll())
        {
            string agentId = $"workflow-{workflowReg.Id}";
            AIAgent workflowAgent = workflowReg.Workflow.AsAIAgent(agentId, workflowReg.Name);

            string aguiRoute = $"/api/agui/{agentId}";
            app.MapAGUI(aguiRoute, workflowAgent);

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
