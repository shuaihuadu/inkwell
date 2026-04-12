using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace Inkwell.Functions;

/// <summary>
/// Inkwell Azure Functions 入口
/// 通过 DurableTask 实现 Agent 的 Serverless 持久化托管（需求 4.2）
///
/// 使用方式：
///   1. 配置 AzureOpenAI__Primary__Endpoint 环境变量
///   2. func start 或部署到 Azure
///
/// 注意：需要 Azure DurableTask Scheduler 服务和 Azure Functions 运行时。
/// </summary>
public static class Program
{
    private const string WriterAgentName = "InkwellWriter";
    private const string CriticAgentName = "InkwellCritic";

    public static void Main(string[] args)
    {
        // 读取配置
        string endpoint = Environment.GetEnvironmentVariable("AzureOpenAI__Primary__Endpoint")
            ?? throw new InvalidOperationException("AzureOpenAI__Primary__Endpoint is required.");
        string deploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__Primary__DeploymentName") ?? "gpt-4o-mini";

        // 创建 LLM 客户端
        AzureOpenAIClient azureClient = new(new Uri(endpoint), new AzureCliCredential());

        // 创建 Agent
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        AIAgent writerAgent = chatClient.AsAIAgent(
            instructions: "你是一名专业内容写手。撰写高质量文章。请用中文回复。",
            name: WriterAgentName);

        AIAgent criticAgent = chatClient.AsAIAgent(
            instructions: "你是一名内容编辑。审核文章质量并提供改进建议。请用中文回复。",
            name: CriticAgentName);

        // 构建 Azure Functions 应用并注册 DurableTask Agent
        using IHost app = FunctionsApplication
            .CreateBuilder(args)
            .ConfigureFunctionsWebApplication()
            .ConfigureDurableAgents(options =>
            {
                options.AddAIAgent(writerAgent, timeToLive: TimeSpan.FromHours(1));
                options.AddAIAgent(criticAgent, timeToLive: TimeSpan.FromHours(1));
            })
            .Build();

        app.Run();
    }
}
