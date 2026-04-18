using Azure.AI.OpenAI;
using Azure.Identity;
using Inkwell.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell.A2AServer;

/// <summary>
/// Inkwell A2A 服务器骨架（需求 4.3）
/// 将 Agent 通过 A2A 协议暴露给远程客户端
///
/// 注意：A2A 托管包（Microsoft.Agents.AI.Hosting.A2A.AspNetCore）尚未发布到 NuGet，
/// 当前仅提供项目结构和 Agent 创建逻辑。待包发布后添加 MapA2A 端点映射。
///
/// 使用方式（包发布后）：
///   1. 设置环境变量 AzureOpenAI__Primary__Endpoint
///   2. dotnet run
///   3. 远程客户端通过 http://localhost:5100/.well-known/agent.json 发现 Agent
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // 读取配置
        string endpoint = builder.Configuration["AzureOpenAI:Primary:Endpoint"]
            ?? Environment.GetEnvironmentVariable("AzureOpenAI__Primary__Endpoint")
            ?? throw new InvalidOperationException("AzureOpenAI:Primary:Endpoint is required.");

        string deploymentName = builder.Configuration["AzureOpenAI:Primary:DeploymentName"]
            ?? Environment.GetEnvironmentVariable("AzureOpenAI__Primary__DeploymentName")
            ?? "gpt-4o-mini";

        // 创建 LLM 客户端
        IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName)
            .AsIChatClient();

        // 复用 InkwellAgents.CreateWriter（包含护栏中间件）避免多个入口各自拼装 Agent
        AgentRegistration writerRegistration = InkwellAgents.CreateWriter(chatClient);
        AIAgent writerAgent = writerRegistration.Agent;

        builder.Services.AddSingleton(writerAgent);

        WebApplication app = builder.Build();

        // 临时端点：直接调用 Agent（A2A 托管包发布后替换为 MapA2A）
        app.MapPost("/run", async (HttpContext context) =>
        {
            using StreamReader reader = new(context.Request.Body);
            string input = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(input))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Input is required.");
                return;
            }

            AIAgent agent = app.Services.GetRequiredService<AIAgent>();
            AgentResponse response = await agent.RunAsync(input);
            await context.Response.WriteAsync(response.Text);
        });

        // TODO: 待 Microsoft.Agents.AI.Hosting.A2A.AspNetCore 发布到 NuGet 后，
        // 替换上方临时端点为：
        //
        // AgentCard agentCard = new()
        // {
        //     Name = "InkwellWriter",
        //     Description = "内容写手",
        //     Version = "1.0.0",
        //     Skills = [new AgentSkill { Id = "write-article", Name = "撰写文章" }]
        // };
        // app.MapA2A(writerAgent, "/", agentCard, tm => app.MapWellKnownAgentCard(tm, "/"));

        app.MapGet("/health", () => Results.Ok(new { status = "healthy", agent = "InkwellWriter" }));

        app.Urls.Add("http://localhost:5100");
        app.Run();
    }
}
