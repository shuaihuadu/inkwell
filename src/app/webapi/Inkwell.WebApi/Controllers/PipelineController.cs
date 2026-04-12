using System.Text.Json;
using Inkwell;
using Inkwell.Workflows;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 流水线控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EditorOrAdmin")]
public sealed class PipelineController(
    WorkflowRegistry workflowRegistry,
    IPipelineRunPersistenceProvider runProvider,
    ILogger<PipelineController> logger) : ControllerBase
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
    /// 启动内容生产流水线（SSE 流式返回事件，支持 Checkpoint）
    /// </summary>
    /// <param name="request">运行请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>SSE 事件流</returns>
    [HttpPost("run")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task RunAsync([FromBody] PipelineRunRequest request, CancellationToken cancellationToken)
    {
        // [H2 修复] 从 WorkflowRegistry 获取，不再每次重建
        WorkflowRegistration? registration = workflowRegistry.GetById("content-pipeline");
        if (registration is null)
        {
            this.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }

        // 创建运行记录
        PipelineRunRecord runRecord = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Topic = request.Topic,
            Status = "Running",
            StartedAt = DateTimeOffset.UtcNow
        };
        await runProvider.AddAsync(runRecord, cancellationToken);

        logger.LogInformation("[Pipeline] 启动流水线 RunId={RunId}, Topic={Topic}", runRecord.Id, request.Topic);

        // 设置 SSE 响应头
        this.Response.ContentType = "text/event-stream";
        this.Response.Headers.CacheControl = "no-cache";
        this.Response.Headers.Connection = "keep-alive";

        // [C3 修复] 使用 using 确保 StreamWriter 被释放
        await using StreamWriter writer = new(this.Response.Body, leaveOpen: true);

        try
        {
            CheckpointManager checkpointManager = CheckpointManager.Default;

            // [H3 修复] 传入 CancellationToken
            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
                registration.Workflow, request.Topic, checkpointManager);

            await foreach (WorkflowEvent evt in run.WatchStreamAsync().WithCancellation(cancellationToken))
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
                    SuperStepCompletedEvent superStep => JsonSerializer.Serialize(new
                    {
                        type = "checkpoint",
                        runId = runRecord.Id,
                        hasCheckpoint = superStep.CompletionInfo?.Checkpoint is not null,
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
            await runProvider.UpdateAsync(runRecord, cancellationToken);

            logger.LogInformation("[Pipeline] 流水线完成 RunId={RunId}", runRecord.Id);

            await writer.WriteLineAsync($"data: {{\"type\":\"done\",\"runId\":\"{runRecord.Id}\"}}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }
        catch (OperationCanceledException)
        {
            // [C4 修复] 客户端断开连接
            runRecord.Status = "Cancelled";
            runRecord.CompletedAt = DateTimeOffset.UtcNow;
            await runProvider.UpdateAsync(runRecord);

            logger.LogWarning("[Pipeline] 流水线被取消 RunId={RunId}", runRecord.Id);
        }
        catch (Exception ex)
        {
            // [C4 修复] Workflow 执行异常
            runRecord.Status = "Failed";
            runRecord.CompletedAt = DateTimeOffset.UtcNow;
            await runProvider.UpdateAsync(runRecord);

            logger.LogError(ex, "[Pipeline] 流水线执行失败 RunId={RunId}", runRecord.Id);

            try
            {
                await writer.WriteLineAsync($"data: {{\"type\":\"error\",\"runId\":\"{runRecord.Id}\",\"message\":\"{ex.Message.Replace("\"", "\\\"").Replace("\n", " ")}\"}}");
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }
            catch
            {
                // 客户端已断开，忽略写入错误
            }
        }
    }
}

/// <summary>
/// 流水线运行请求
/// </summary>
public sealed class PipelineRunRequest
{
    /// <summary>
    /// 获取或设置文章主题
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置最大修订次数
    /// </summary>
    public int MaxRevisions { get; set; } = 3;
}
