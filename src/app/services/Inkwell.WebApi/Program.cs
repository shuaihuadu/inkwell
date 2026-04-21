using Inkwell;
using Inkwell.Agents;
using Inkwell.Agents.Middleware;
using Inkwell.Persistence.InMemory;
using Inkwell.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace Inkwell.WebApi;

/// <summary>
/// Inkwell Web API 入口
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // ---------- DI 注册 ----------
        builder.Services.AddControllers();

        builder.Services.AddInkwellCore()
            .UseInMemoryDatabase()
            .UseAzureOpenAI(builder.Configuration)
            .UseAzureOpenAIEmbedding(builder.Configuration);

        IChatClient primaryClient = builder.Services.FindKeyedSingletonInstance<IChatClient>(ModelServiceKeys.Primary)
            ?? throw new InvalidOperationException("Primary IChatClient not registered. Ensure UseAzureOpenAI() is called before this point.");

        // 向量存储 + 记忆
        VectorStore vectorStore = new InMemoryVectorStore();
        builder.Services.AddSingleton(vectorStore);
        builder.Services.AddSingleton(new AgentMemoryService(vectorStore));

        // Agent
        AgentRegistry agentRegistry = builder.Services.AddInkwellAgents(builder.Configuration);

        // 知识库（先种子；EmbeddingGenerator 在 Build 后注入）
        KnowledgeBaseService knowledgeBase = new(vectorStore);
        WebApiStartup.SeedKnowledgeBase(knowledgeBase);
        builder.Services.AddSingleton(knowledgeBase);

        builder.Services.AddSingleton<KnowledgePersistenceService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<KnowledgePersistenceService>());

        builder.Services.AddSingleton<CmsMcpTools>();
        builder.Services.AddInkwellAuth(builder.Configuration);
        WorkflowRegistry workflowRegistry = builder.Services.AddInkwellWorkflows();
        builder.Services.AddSingleton<HitlPendingRegistry>();
        builder.Services.AddAGUI();

        builder.AddServiceDefaults();

        builder.Services.Configure<SessionCleanupOptions>(
            builder.Configuration.GetSection(SessionCleanupOptions.SectionName));
        builder.Services.AddHostedService<SessionCleanupService>();

        builder.AddInkwellCors();

        // ---------- Build ----------
        WebApplication app = builder.Build();

        // 为文章写入网关注入 Scope 工厂：
        // ReviewGateExecutor 是单例，无法直接依赖 Scoped 的 IArticlePersistenceProvider，
        // 这里在 DI 构建完成后把 ScopeFactory 回填到 Gateway，让它在持久化时按需创建 Scope。
        ArticleWriteGateway articleWriteGateway = app.Services.GetRequiredService<ArticleWriteGateway>();
        articleWriteGateway.ScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

        // 加载声明式 Agent / Workflow（YAML），使用宿主 LoggerFactory
        WebApiStartup.LoadDeclarativeArtifacts(app, agentRegistry, workflowRegistry, primaryClient);

        // 运行时注入 EmbeddingGenerator 到知识库
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGen =
            app.Services.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        if (embeddingGen is not null)
        {
            knowledgeBase.SetEmbeddingGenerator(embeddingGen);
        }

        // ---------- 中间件管线 ----------
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error");
        }

        app.UseCors();

        // 仅在 Auth 启用时插入认证中间件（关闭时无 Scheme 注册，UseAuthentication 会抛异常）
        AuthOptions authOpts = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new();
        if (authOpts.Enabled)
        {
            app.UseAuthentication();
        }

        app.UseAuthorization();
        app.MapControllers();
        app.MapDefaultEndpoints();

        // 配置 Guardrail 静态日志记录器（中间件签名固定无法注入）
        ContentGuardrailMiddleware.Configure(
            app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Inkwell.Guardrail"));

        // ---------- AG-UI 端点映射 ----------
        ISessionPersistenceProvider sessionProvider = app.Services.GetRequiredService<ISessionPersistenceProvider>();

        // 明确从 Keyed Primary 拿 title client，避免未来引入其他非 Keyed IChatClient 后随机命中
        IChatClient? titleClient = app.Services.GetKeyedService<IChatClient>(ModelServiceKeys.Primary);

        WebApiStartup.MapAgentAguiEndpoints(app, agentRegistry, sessionProvider, titleClient);
        WebApiStartup.MapWorkflowAguiEndpoints(app, workflowRegistry, agentRegistry, sessionProvider, titleClient);

        app.Run();
    }
}