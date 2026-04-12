using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Inkwell;
using Inkwell.Workflows;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 流水线控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PipelineController(
    IConfiguration configuration,
    IPipelineRunPersistenceProvider runProvider) : ControllerBase
{
    /// <summary>
    /// 获取最近的运行记录
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>运行记录列表</returns>
    [HttpGet("runs")]
    [ProducesResponseType(typeof(IReadOnlyList<PipelineRunRecord>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentRunsAsync([FromQuery] int count = 20, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PipelineRunRecord> runs = await runProvider.GetRecentAsync(count, cancellationToken);
        return this.Ok(runs);
    }

    /// <summary>
    /// 根据 ID 获取运行记录
    /// </summary>
    /// <param name="id">运行记录 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>运行记录</returns>
    [HttpGet("runs/{id}")]
    [ProducesResponseType(typeof(PipelineRunRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunByIdAsync(string id, CancellationToken cancellationToken)
    {
        PipelineRunRecord? run = await runProvider.GetByIdAsync(id, cancellationToken);
        return run is not null ? this.Ok(run) : this.NotFound();
    }

    /// <summary>
    /// 启动内容生产流水线（SSE 流式返回事件）
    /// </summary>
    /// <param name="request">运行请求</param>
    /// <returns>SSE 事件流</returns>
    [HttpPost("run")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task RunAsync([FromBody] PipelineRunRequest request)
    {
        string endpoint = configuration["AZURE_OPENAI_ENDPOINT"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not configured.");
        string deploymentName = configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? "gpt-4o-mini";

        IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName)
            .AsIChatClient();

        Workflow workflow = ContentPipelineBuilder.Build(chatClient, maxRevisions: request.MaxRevisions);

        // 创建运行记录
        PipelineRunRecord runRecord = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Topic = request.Topic,
            Status = "Running",
            StartedAt = DateTimeOffset.UtcNow
        };
        await runProvider.AddAsync(runRecord);

        // 设置 SSE 响应头
        this.Response.ContentType = "text/event-stream";
        this.Response.Headers.CacheControl = "no-cache";
        this.Response.Headers.Connection = "keep-alive";

        StreamWriter writer = new(this.Response.Body);

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, request.Topic);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            string eventData = evt switch
            {
                WorkflowOutputEvent output => JsonSerializer.Serialize(new
                {
                    type = "output",
                    runId = runRecord.Id,
                    data = output.Data?.ToString(),
                    executorId = output.ExecutorId,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }),
                ExecutorCompletedEvent completed => JsonSerializer.Serialize(new
                {
                    type = "executor_complete",
                    runId = runRecord.Id,
                    executorId = completed.ExecutorId,
                    data = completed.Data?.ToString(),
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }),
                RequestInfoEvent requestInfo => JsonSerializer.Serialize(new
                {
                    type = "human_review_request",
                    runId = runRecord.Id,
                    requestId = requestInfo.Request.RequestId,
                    data = requestInfo.Request.Data?.ToString(),
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(eventData))
            {
                await writer.WriteLineAsync($"data: {eventData}");
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }
        }

        // 更新运行记录状态
        runRecord.Status = "Completed";
        runRecord.CompletedAt = DateTimeOffset.UtcNow;
        await runProvider.UpdateAsync(runRecord);

        await writer.WriteLineAsync($"data: {{\"type\":\"done\",\"runId\":\"{runRecord.Id}\"}}");
        await writer.WriteLineAsync();
        await writer.FlushAsync();
    }
}

/// <summary>
/// 流水线运行请求
/// </summary>
/// <param name="Topic">文章主题</param>
/// <param name="MaxRevisions">最大修订次数</param>
public sealed record PipelineRunRequest(string Topic, int MaxRevisions = 3);
