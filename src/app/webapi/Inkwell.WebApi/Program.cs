using Azure.AI.OpenAI;
using Azure.Identity;
using Inkwell;
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

        // 注册 Inkwell 核心服务 + 持久化
        // 方式一：Fluent API（显式指定）
        builder.Services.AddInkwellCore().UseInMemoryDatabase();

        // 方式二：配置文件驱动（从 appsettings.json 的 Persistence 节点读取）
        // builder.Services.AddInkwellCore().UseConfiguredPersistence(builder.Configuration);

        // 配置 CORS（允许前端开发服务器访问）
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

        // ========== AG-UI 端点 ==========
        string endpoint = app.Configuration["AZURE_OPENAI_ENDPOINT"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not configured.");
        string deploymentName = app.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? "gpt-4o-mini";

        IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName)
            .AsIChatClient();

        // AG-UI 使用 ChatClientAgent（支持 ChatProtocol）
        // 复杂 Workflow（Fan-Out/Fan-In/HITL）通过 /api/pipeline/run SSE 端点访问
        AIAgent inkwellAgent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    你是 Inkwell 内容创作助手。你帮助用户：
                    1. 分析文章主题的市场趋势和目标受众
                    2. 撰写高质量的文章内容
                    3. 审核和优化文章质量
                    请用中文回复，内容要专业、有深度、有吸引力。
                    """
            }
        });

        // 映射 AG-UI 端点（POST /api/agui）
        app.MapAGUI("/api/agui", inkwellAgent);

        app.Run();
    }
}
