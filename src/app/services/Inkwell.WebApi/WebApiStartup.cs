using Inkwell;
using Inkwell.Agents;
using Inkwell.Agents.Middleware;
using Inkwell.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

namespace Inkwell.WebApi;

/// <summary>
/// Web API 启动期辅助方法
/// 把 Program.Main 中较重的注册 / 映射逻辑拆分到这里以便维护
/// </summary>
internal static class WebApiStartup
{
    private const string DefaultDevOriginA = "http://localhost:5188";
    private const string DefaultDevOriginB = "http://localhost:3000";

    /// <summary>
    /// 配置 CORS（origin 从配置读取，缺省回退到本地开发地址）
    /// </summary>
    public static void AddInkwellCors(this WebApplicationBuilder builder)
    {
        string[] corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? [DefaultDevOriginA, DefaultDevOriginB];

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
    }

    /// <summary>
    /// 给知识库写入种子数据（仅当尚未从 DB 中加载到任何文档时执行）
    /// </summary>
    public static void SeedKnowledgeBase(KnowledgeBaseService kb)
    {
        kb.AddDocument("Inkwell 品牌风格指南", """
            Inkwell 是一个 AI 驱动的内容生产平台。
            品牌调性：专业、创新、高效。
            文章风格：结构清晰、数据驱动、面向行动。
            目标读者：内容创作者、营销人员、企业编辑。
            """);
        kb.AddDocument("SEO 最佳实践 2026", """
            1. 标题控制在 60 字符以内，包含主关键词
            2. 元描述 150-160 字符，包含行动号召
            3. 正文前 100 字出现核心关键词
            4. 使用 H2/H3 结构化内容
            5. 图片包含 ALT 文本
            6. 内链和外链平衡
            """);
    }

    /// <summary>
    /// 加载声明式 Agent / Workflow（YAML 文件），使用宿主 LoggerFactory
    /// </summary>
    public static void LoadDeclarativeArtifacts(
        WebApplication app,
        AgentRegistry agentRegistry,
        WorkflowRegistry workflowRegistry,
        IChatClient primaryClient)
    {
        ILogger logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Inkwell.Declarative");

        string agentsDir = Path.Combine(app.Environment.ContentRootPath, "Agents");
        int loadedAgents = DeclarativeAgentLoader.LoadFromDirectory(agentRegistry, primaryClient, agentsDir, logger);
        logger.LogInformation("Loaded {Count} declarative agents from {Directory}", loadedAgents, agentsDir);

        string workflowsDir = Path.Combine(app.Environment.ContentRootPath, "Workflows");
        int loadedWorkflows = DeclarativeWorkflowLoader.LoadFromDirectory(workflowRegistry, primaryClient, workflowsDir, logger);
        logger.LogInformation("Loaded {Count} declarative workflows from {Directory}", loadedWorkflows, workflowsDir);
    }

    /// <summary>
    /// 把所有已注册 Agent 映射到 AG-UI 端点（带会话持久化、要求授权）
    /// </summary>
    public static void MapAgentAguiEndpoints(
        WebApplication app,
        AgentRegistry agentRegistry,
        ISessionPersistenceProvider sessionProvider,
        IChatClient? titleClient)
    {
        foreach (AgentRegistration registration in agentRegistry.GetAll())
        {
            AIAgent wrapped = registration.Agent.WithSessionPersistence(registration.Id, sessionProvider, titleClient);
            app.MapAGUI(registration.AguiRoute, wrapped)
                .RequireAuthorization(InkwellPolicies.EditorOrAdmin);
        }
    }

    /// <summary>
    /// 把所有已注册 Workflow 适配为 ChatClientAgent 并映射 AG-UI 端点
    /// 同时把 wrapped agent 反向注册回 AgentRegistry，方便统一查询/清理
    /// </summary>
    public static void MapWorkflowAguiEndpoints(
        WebApplication app,
        WorkflowRegistry workflowRegistry,
        AgentRegistry agentRegistry,
        ISessionPersistenceProvider sessionProvider,
        IChatClient? titleClient)
    {
        ILogger<WorkflowChatClient> workflowChatLogger =
            app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<WorkflowChatClient>();

        HitlPendingRegistry hitlRegistry = app.Services.GetRequiredService<HitlPendingRegistry>();

        foreach (WorkflowRegistration workflowReg in workflowRegistry.GetAll())
        {
            string agentId = $"workflow-{workflowReg.Id}";

            // 用 WorkflowChatClient 将 Workflow 适配为 IChatClient，再包装为 ChatClientAgent
            // 这样 Agent 完整支持 ChatProtocol（List + TurnToken），AG-UI 端点可正常工作
            // 同时把 WorkflowCapabilities 传入，决定多轮/HITL 行为
            WorkflowChatClient workflowClient = new(
                workflowReg.Workflow,
                workflowReg.Capabilities,
                workflowChatLogger,
                hitlRegistry);
            AIAgent workflowAgent = new ChatClientAgent(workflowClient, new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = $"你是 Inkwell 平台的 [{workflowReg.Name}] Workflow。{workflowReg.Description}"
                }
            });

            string aguiRoute = $"/api/agui/{agentId}";
            AIAgent wrapped = workflowAgent.WithSessionPersistence(agentId, sessionProvider, titleClient);
            app.MapAGUI(aguiRoute, wrapped)
                .RequireAuthorization(InkwellPolicies.EditorOrAdmin);

            agentRegistry.Register(new AgentRegistration
            {
                Id = agentId,
                Name = $"[Workflow] {workflowReg.Name}",
                Description = workflowReg.Description,
                Agent = workflowAgent,
                AguiRoute = aguiRoute
            });
        }
    }
}
