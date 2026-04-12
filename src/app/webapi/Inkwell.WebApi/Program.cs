using Azure.AI.OpenAI;
using Azure.Identity;
using Inkwell;
using Inkwell.Agents;
using Inkwell.Persistence.InMemory;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
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
        builder.Services.AddInkwellCore().UseInMemoryDatabase();

        // 创建 LLM 客户端
        string endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not configured.");
        string deploymentName = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? "gpt-4o-mini";

        IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName)
            .AsIChatClient();

        // 注册所有 Agent
        AgentRegistry agentRegistry = builder.Services.AddInkwellAgents(chatClient);

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
