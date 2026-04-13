using Inkwell;
using Inkwell.Agents;
using Inkwell.Agents.Middleware;
using Inkwell.Persistence.InMemory;
using Inkwell.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.WebApi;

/// <summary>
/// Inkwell Web API 入口
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // 注册 Controller
        builder.Services.AddControllers();

        // 注册 Inkwell 核心服务 + 持久化 + Azure OpenAI 多模型 + Embedding + 向量存储
        builder.Services.AddInkwellCore()
            .UseInMemoryDatabase()
            .UseAzureOpenAI(builder.Configuration)
            .UseAzureOpenAIEmbedding(builder.Configuration);

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

        // 注册 Agent 记忆服务（在 AddInkwellAgents 之前，以便 Agent 挂载长期记忆）
        // 使用 DI 注册的 VectorStore 实例（此时 IEmbeddingGenerator 会在运行时延迟解析）
        Microsoft.Extensions.VectorData.VectorStore vectorStore =
            new Microsoft.SemanticKernel.Connectors.InMemory.InMemoryVectorStore();
        builder.Services.AddSingleton(vectorStore);
        AgentMemoryService agentMemory = new(vectorStore);
        builder.Services.AddSingleton(agentMemory);

        // 注册所有 Agent（使用 Keyed IChatClient）
        AgentRegistry agentRegistry = builder.Services.AddInkwellAgents(builder.Configuration);

        // 注册知识库服务 + 初始化示例文档
        KnowledgeBaseService knowledgeBase = new();
        knowledgeBase.AddDocument("Inkwell 品牌风格指南", """
            Inkwell 是一个 AI 驱动的内容生产平台。
            品牌调性：专业、创新、高效。
            文章风格：结构清晰、数据驱动、面向行动。
            目标读者：内容创作者、营销人员、企业编辑。
            """);
        knowledgeBase.AddDocument("SEO 最佳实践 2026", """
            1. 标题控制在 60 字符以内，包含主关键词
            2. 元描述 150-160 字符，包含行动号召
            3. 正文前 100 字出现核心关键词
            4. 使用 H2/H3 结构化内容
            5. 图片包含 ALT 文本
            6. 内链和外链平衡
            """);
        builder.Services.AddSingleton(knowledgeBase);

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

        // 注册 AG-UI 服务（JSON 序列化配置）
        builder.Services.AddAGUI();

        // 添加 Aspire 服务默认配置（OpenTelemetry、健康检查、服务发现、弹性 HTTP）
        builder.AddServiceDefaults();

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
        app.MapDefaultEndpoints();

        // ========== 为每个 Agent 映射 AG-UI 端点（带会话持久化）==========
        ISessionPersistenceProvider sessionProvider = app.Services.GetRequiredService<ISessionPersistenceProvider>();

        foreach (AgentRegistration registration in agentRegistry.GetAll())
        {
            AIAgent wrappedAgent = registration.Agent.WithSessionPersistence(registration.Id, sessionProvider);
            app.MapAGUI(registration.AguiRoute, wrappedAgent);
        }

        // ========== 将 Workflow 包装为 Agent 并映射 AG-UI 端点（带会话持久化）==========
        foreach (WorkflowRegistration workflowReg in workflowRegistry.GetAll())
        {
            string agentId = $"workflow-{workflowReg.Id}";
            AIAgent workflowAgent = workflowReg.Workflow.AsAIAgent(agentId, workflowReg.Name, includeWorkflowOutputsInResponse: true);

            string aguiRoute = $"/api/agui/{agentId}";
            AIAgent wrappedWorkflowAgent = workflowAgent.WithSessionPersistence(agentId, sessionProvider);
            app.MapAGUI(aguiRoute, wrappedWorkflowAgent);

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
