using System.Text.Json;
using Inkwell.Workflows;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// Workflow 管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EditorOrAdmin")]
public sealed class WorkflowsController(
    WorkflowRegistry workflowRegistry,
    ILogger<WorkflowsController> logger) : ControllerBase
{
    /// <summary>
    /// 获取所有 Workflow 列表
    /// </summary>
    /// <returns>Workflow 列表</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        IReadOnlyList<WorkflowRegistration> workflows = workflowRegistry.GetAll();

        return this.Ok(workflows.Select(w => new
        {
            w.Id,
            w.Name,
            w.Description
        }));
    }

    /// <summary>
    /// 获取 Workflow 详情
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <returns>Workflow 详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(string id)
    {
        WorkflowRegistration? registration = workflowRegistry.GetById(id);

        if (registration is null)
        {
            return this.NotFound();
        }

        return this.Ok(new
        {
            registration.Id,
            registration.Name,
            registration.Description
        });
    }

    /// <summary>
    /// 获取 Workflow 拓扑图（Mermaid 格式）
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <returns>Mermaid 格式的拓扑图字符串</returns>
    [HttpGet("{id}/topology")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetTopology(string id)
    {
        WorkflowRegistration? registration = workflowRegistry.GetById(id);

        if (registration is null)
        {
            return this.NotFound();
        }

        string mermaid = registration.Workflow.ToMermaidString();

        return this.Ok(new
        {
            registration.Id,
            registration.Name,
            format = "mermaid",
            topology = mermaid
        });
    }

    /// <summary>
    /// 运行指定 Workflow（SSE 流式事件，支持 Checkpoint）
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <param name="request">运行请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>SSE 事件流</returns>
    [HttpPost("{id}/run")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task RunWorkflowAsync(string id, [FromBody] WorkflowRunRequest request, CancellationToken cancellationToken)
    {
        WorkflowRegistration? registration = workflowRegistry.GetById(id);

        if (registration is null)
        {
            this.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        logger.LogInformation("[Workflow] 启动 Workflow Id={WorkflowId}, Input={Input}", id, request.Input[..Math.Min(request.Input.Length, 100)]);

        // 设置 SSE 响应头
        this.Response.ContentType = "text/event-stream";
        this.Response.Headers.CacheControl = "no-cache";
        this.Response.Headers.Connection = "keep-alive";

        // [C3 修复] using 确保 StreamWriter 释放
        await using StreamWriter writer = new(this.Response.Body, leaveOpen: true);

        try
        {
            CheckpointManager checkpointManager = CheckpointManager.Default;

            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
                registration.Workflow, request.Input, checkpointManager);

            // [H3 修复] 传入 CancellationToken
            await foreach (WorkflowEvent evt in run.WatchStreamAsync().WithCancellation(cancellationToken))
            {
                string eventData = evt switch
                {
                    WorkflowOutputEvent output => JsonSerializer.Serialize(new
                    {
                        type = "output",
                        workflowId = id,
                        data = output.Data?.ToString(),
                        executorId = output.ExecutorId
                    }),
                    ExecutorCompletedEvent completed => JsonSerializer.Serialize(new
                    {
                        type = "executor_complete",
                        workflowId = id,
                        executorId = completed.ExecutorId,
                        data = completed.Data?.ToString()
                    }),
                    SuperStepCompletedEvent superStep => JsonSerializer.Serialize(new
                    {
                        type = "checkpoint",
                        workflowId = id,
                        hasCheckpoint = superStep.CompletionInfo?.Checkpoint is not null
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

            logger.LogInformation("[Workflow] 完成 Id={WorkflowId}", id);

            await writer.WriteLineAsync($"data: {{\"type\":\"done\",\"workflowId\":\"{id}\"}}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[Workflow] 被取消 Id={WorkflowId}", id);
        }
        catch (Exception ex)
        {
            // [C4 修复] 异常处理 + 错误事件推送
            logger.LogError(ex, "[Workflow] 执行失败 Id={WorkflowId}", id);

            try
            {
                await writer.WriteLineAsync($"data: {{\"type\":\"error\",\"workflowId\":\"{id}\",\"message\":\"{ex.Message.Replace("\"", "\\\"").Replace("\n", " ")}\"}}");
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }
            catch
            {
                // 客户端已断开
            }
        }
    }
}

/// <summary>
/// Workflow 运行请求
/// </summary>
public sealed class WorkflowRunRequest
{
    /// <summary>
    /// 获取或设置输入内容
    /// </summary>
    public string Input { get; set; } = string.Empty;
}
