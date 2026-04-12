using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Inkwell.Core;
using Inkwell.Workflows;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Inkwell.ConsoleApp;

/// <summary>
/// Inkwell 内容生产流水线 - 控制台入口
/// </summary>
internal static class Program
{
    private static async Task Main(string[] args)
    {
        // 初始化配置
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<UserSecretsMarker>()
            .AddEnvironmentVariables()
            .Build();

        string endpoint = configuration["AZURE_OPENAI_ENDPOINT"]
            ?? throw new InvalidOperationException("Please set AZURE_OPENAI_ENDPOINT in user-secrets or environment variables.");
        string deploymentName = configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? "gpt-4o-mini";

        // 创建 LLM 客户端
        IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName)
            .AsIChatClient();

        // 构建 Workflow
        Workflow workflow = ContentPipelineBuilder.Build(chatClient, maxRevisions: 3);

        // 获取输入
        string topic = args.Length > 0
            ? string.Join(" ", args)
            : PromptForInput("请输入文章主题: ");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n========== Inkwell 内容生产流水线 ==========");
        Console.WriteLine($"主题: {topic}");
        Console.WriteLine($"============================================\n");
        Console.ResetColor();

        // 执行 Workflow
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, topic);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case WorkflowOutputEvent output:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n[输出] {output.Data}");
                    Console.ResetColor();
                    break;

                case RequestInfoEvent requestInfo:
                    await HandleHumanReviewAsync(requestInfo, run);
                    break;

                case ExecutorCompletedEvent completed:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  [{completed.ExecutorId}] 完成");
                    Console.ResetColor();
                    break;
            }
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n========== 流水线执行完毕 ==========\n");
        Console.ResetColor();
    }

    /// <summary>
    /// 处理人工审核环节
    /// </summary>
    private static async Task HandleHumanReviewAsync(RequestInfoEvent requestInfo, StreamingRun run)
    {
        if (requestInfo.Request.TryGetDataAs<Article>(out Article? article))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n╔══════════════════════════════════════╗");
            Console.WriteLine("║         人工审核                      ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine($"标题: {article.Title}");
            Console.WriteLine($"版本: 第 {article.Revision} 稿");
            Console.WriteLine($"状态: {article.Status}");
            Console.WriteLine("─────────────────────────────────────");
            Console.WriteLine(article.Content);
            Console.WriteLine("─────────────────────────────────────");

            string input = PromptForInput("是否批准发布？(y/n): ");
            bool approved = input.Equals("y", StringComparison.OrdinalIgnoreCase)
                         || input.Equals("yes", StringComparison.OrdinalIgnoreCase);

            ExternalResponse response = requestInfo.Request.CreateResponse(approved);
            await run.SendResponseAsync(response);

            Console.ForegroundColor = approved ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(approved ? "→ 已批准发布" : "→ 已退回修改");
            Console.ResetColor();
        }
    }

    private static string PromptForInput(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }
}

/// <summary>
/// UserSecrets 标记类型
/// </summary>
internal sealed class UserSecretsMarker;
