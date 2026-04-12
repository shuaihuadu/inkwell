using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DurableTask;
using Microsoft.DurableTask.Client.AzureManaged;
using Microsoft.DurableTask.Worker.AzureManaged;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inkwell.DurableHost;

/// <summary>
/// Inkwell DurableTask Console 主机
/// 演示 Agent 和 Workflow 的持久化托管（需求 4.1）
/// 
/// 使用方式：
///   1. 设置环境变量 DTS_CONNECTION_STRING（DurableTask Scheduler 连接字符串）
///   2. 设置环境变量 AZURE_OPENAI_ENDPOINT
///   3. dotnet run
///
/// 注意：此项目需要 Azure DurableTask Scheduler 服务。
/// 若无 DTS 服务，可通过 InMemory 模式运行（见注释部分）。
/// </summary>
public static class Program
{
    private const string WriterAgentName = "InkwellWriter";

    public static async Task Main(string[] args)
    {
        // 读取配置
        string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT environment variable is required.");

        string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

        string? dtsConnectionString = Environment.GetEnvironmentVariable("DTS_CONNECTION_STRING");

        // 创建 LLM 客户端
        IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName)
            .AsIChatClient();

        // 创建 Writer Agent
        AIAgent writerAgent = chatClient.AsAIAgent(
            name: WriterAgentName,
            instructions: """
                你是一名专业内容写手。撰写引人入胜、结构清晰的文章。
                请用中文回复。
                """);

        // 构建主机
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        if (!string.IsNullOrWhiteSpace(dtsConnectionString))
        {
            // 使用 Azure DurableTask Scheduler（生产模式）
            builder.Services.ConfigureDurableAgents(
                options => options.AddAIAgent(writerAgent, timeToLive: TimeSpan.FromHours(1)),
                workerBuilder: workerBuilder => workerBuilder.UseDurableTaskScheduler(dtsConnectionString),
                clientBuilder: clientBuilder => clientBuilder.UseDurableTaskScheduler(dtsConnectionString));
        }
        else
        {
            Console.WriteLine("[警告] DTS_CONNECTION_STRING 未设置，DurableTask 功能不可用。");
            Console.WriteLine("[提示] 请设置 Azure DurableTask Scheduler 连接字符串后重试。");
            Console.WriteLine();

            // 仅注册 Agent 到 DI（不启用持久化编排）
            builder.Services.AddKeyedSingleton<AIAgent>(WriterAgentName, writerAgent);
        }

        using IHost host = builder.Build();

        Console.WriteLine("========================================");
        Console.WriteLine(" Inkwell DurableTask Console Host");
        Console.WriteLine("========================================");
        Console.WriteLine($"Agent: {WriterAgentName}");
        Console.WriteLine($"DurableTask: {(!string.IsNullOrWhiteSpace(dtsConnectionString) ? "已启用" : "未启用")}");
        Console.WriteLine();

        if (!string.IsNullOrWhiteSpace(dtsConnectionString))
        {
            // DurableTask 模式：通过 Keyed DI 获取代理
            await host.StartAsync();

            AIAgent agentProxy = host.Services.GetRequiredKeyedService<AIAgent>(WriterAgentName);
            AgentSession session = await agentProxy.CreateSessionAsync();

            Console.WriteLine("请输入写作主题（输入 'exit' 退出）：");

            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                AgentResponse response = await agentProxy.RunAsync(message: input, session: session);
                Console.WriteLine();
                Console.WriteLine(response.Text);
                Console.WriteLine();
            }

            await host.StopAsync();
        }
        else
        {
            // 非 DurableTask 模式：直接调用 Agent
            Console.WriteLine("请输入写作主题（输入 'exit' 退出）：");

            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                AgentResponse response = await writerAgent.RunAsync(input);
                Console.WriteLine();
                Console.WriteLine(response.Text);
                Console.WriteLine();
            }
        }
    }
}
