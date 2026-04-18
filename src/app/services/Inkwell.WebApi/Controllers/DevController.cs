using Inkwell.Agents;
using Inkwell.Workflows;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 开发调试控制器（仅开发环境可用）
/// 提供 Agent/Workflow 的调试信息查看
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = InkwellPolicies.AdminOnly)]
public sealed class DevController(
    AgentRegistry agentRegistry,
    WorkflowRegistry workflowRegistry,
    IChatClient chatClient,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// 获取系统诊断信息
    /// </summary>
    /// <returns>系统诊断数据</returns>
    [HttpGet("diagnostics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetDiagnostics()
    {
        if (!environment.IsDevelopment())
        {
            return this.Forbid();
        }

        return this.Ok(new
        {
            environment = environment.EnvironmentName,
            agents = agentRegistry.GetAll().Select(a => new
            {
                a.Id,
                a.Name,
                a.Description,
                a.AguiRoute,
                agentType = a.Agent.GetType().Name
            }),
            workflows = workflowRegistry.GetAll().Select(w => new
            {
                w.Id,
                w.Name,
                w.Description
            }),
            configuration = new
            {
                azureOpenAI = new
                {
                    primaryDeployment = configuration["AzureOpenAI:Primary:DeploymentName"],
                    secondaryDeployment = configuration["AzureOpenAI:Secondary:DeploymentName"],
                    embeddingDeployment = configuration["AzureOpenAI:Embedding:DeploymentName"],
                    // 不暴露 Endpoint 和 ApiKey
                },
                auth = new
                {
                    enabled = configuration.GetValue<bool>("Auth:Enabled")
                },
                cors = configuration.GetSection("Cors:Origins").Get<string[]>()
            }
        });
    }

    /// <summary>
    /// 获取指定 Agent 的详细调试信息
    /// </summary>
    /// <param name="agentId">Agent ID</param>
    /// <returns>Agent 调试信息</returns>
    [HttpGet("agents/{agentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAgentDebugInfo(string agentId)
    {
        if (!environment.IsDevelopment())
        {
            return this.Forbid();
        }

        AgentRegistration? registration = agentRegistry.GetById(agentId);
        if (registration is null)
        {
            return this.NotFound();
        }

        return this.Ok(new
        {
            registration.Id,
            registration.Name,
            registration.Description,
            registration.AguiRoute,
            agentType = registration.Agent.GetType().FullName,
            // Agent 内部元数据（Type 信息）
            capabilities = new
            {
                hasTools = registration.Description.Contains("工具") || registration.Description.Contains("Tool"),
                hasStructuredOutput = registration.Description.Contains("结构化"),
                hasMiddleware = registration.Description.Contains("护栏") || registration.Description.Contains("审计"),
                isDeclarative = registration.Description.StartsWith("[声明式]"),
                isWorkflowAgent = registration.Id.StartsWith("workflow-")
            }
        });
    }

    /// <summary>
    /// 获取所有 Workflow 的拓扑图汇总
    /// </summary>
    /// <returns>所有 Workflow 的 Mermaid 拓扑</returns>
    [HttpGet("workflows/topologies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAllTopologies()
    {
        if (!environment.IsDevelopment())
        {
            return this.Forbid();
        }

        IReadOnlyList<WorkflowRegistration> workflows = workflowRegistry.GetAll();

        return this.Ok(workflows.Select(w =>
        {
            string mermaid;
            try
            {
                mermaid = w.Workflow.ToMermaidString();
            }
            catch
            {
                mermaid = "// Error generating topology";
            }

            return new
            {
                w.Id,
                w.Name,
                mermaid
            };
        }));
    }

    /// <summary>
    /// 诊断：直接测试 IChatClient 流式输出，隔离 Agent Framework 层
    /// </summary>
    /// <param name="prompt">测试提示词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>SSE 流，每个 chunk 一个事件，含序号和内容长度</returns>
    [HttpGet("stream-test")]
    public async Task StreamTest([FromQuery] string prompt = "用中文写一篇关于AI的短文，200字左右", CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            this.Response.StatusCode = 403;
            return;
        }

        this.Response.ContentType = "text/event-stream";
        this.Response.Headers.CacheControl = "no-cache";
        this.Response.Headers.Connection = "keep-alive";

        await using StreamWriter writer = new(this.Response.Body, leaveOpen: true);

        List<ChatMessage> messages =
        [
            new(ChatRole.User, prompt)
        ];

        int chunkIndex = 0;
        int totalChars = 0;

        await foreach (ChatResponseUpdate update in chatClient.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
        {
            string text = update.Text ?? "";
            totalChars += text.Length;
            chunkIndex++;

            await writer.WriteLineAsync($"data: [chunk {chunkIndex}] len={text.Length} total={totalChars} text=\"{text.Replace("\"", "\\\"").Replace("\n", "\\n")}\"");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        await writer.WriteLineAsync($"data: [done] totalChunks={chunkIndex} totalChars={totalChars}");
        await writer.WriteLineAsync();
        await writer.FlushAsync();
    }

    /// <summary>
    /// 诊断：测试 Agent RunStreamingAsync 输出，验证 Agent Framework 是否缓冲
    /// </summary>
    /// <param name="agentId">Agent ID</param>
    /// <param name="prompt">测试提示词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>SSE 流，每个 AgentResponseUpdate 一个事件</returns>
    [HttpGet("agent-stream-test/{agentId}")]
    public async Task AgentStreamTest(string agentId, [FromQuery] string prompt = "hello", CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            this.Response.StatusCode = 403;
            return;
        }

        this.Response.ContentType = "text/event-stream";
        this.Response.Headers.CacheControl = "no-cache";
        this.Response.Headers.Connection = "keep-alive";

        await using StreamWriter writer = new(this.Response.Body, leaveOpen: true);

        AgentRegistration? registration = agentRegistry.GetById(agentId);
        if (registration is null)
        {
            await writer.WriteLineAsync($"data: [error] agent '{agentId}' not found");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
            return;
        }

        List<ChatMessage> messages = [new(ChatRole.User, prompt)];

        int updateIndex = 0;
        await foreach (Microsoft.Agents.AI.AgentResponseUpdate update in registration.Agent.RunStreamingAsync(messages, cancellationToken: cancellationToken))
        {
            updateIndex++;
            string textContent = "";
            foreach (AIContent content in update.Contents)
            {
                if (content is TextContent tc)
                {
                    textContent += tc.Text;
                }
            }

            await writer.WriteLineAsync($"data: [update {updateIndex}] contentCount={update.Contents.Count} textLen={textContent.Length} text=\"{textContent.Replace("\"", "\\\"").Replace("\n", "\\n")[..Math.Min(textContent.Length, 100)]}\"");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        await writer.WriteLineAsync($"data: [done] totalUpdates={updateIndex}");
        await writer.WriteLineAsync();
        await writer.FlushAsync();
    }
}
