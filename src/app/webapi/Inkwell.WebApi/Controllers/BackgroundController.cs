using Inkwell.Agents;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 后台任务控制器
/// 演示长时间运行的 Agent 后台响应（需求 2.13）和工具循环检查点（需求 2.16）
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EditorOrAdmin")]
public sealed class BackgroundController(
    AgentRegistry agentRegistry,
    ISessionPersistenceService sessionService,
    ILogger<BackgroundController> logger) : ControllerBase
{
    /// <summary>
    /// 提交后台任务（深度研究报告生成）
    /// 使用 AllowBackgroundResponses 模式，客户端可通过 taskId 轮询结果
    /// </summary>
    /// <param name="request">任务请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务 ID</returns>
    [HttpPost("submit")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SubmitAsync([FromBody] BackgroundTaskRequest request, CancellationToken cancellationToken)
    {
        AgentRegistration? registration = agentRegistry.GetById(request.AgentId);
        if (registration is null)
        {
            return this.NotFound($"Agent '{request.AgentId}' not found.");
        }

        string taskId = Guid.NewGuid().ToString("N");

        logger.LogInformation("[Background] 提交后台任务 TaskId={TaskId}, AgentId={AgentId}", taskId, request.AgentId);

        // 在后台线程执行 Agent（不阻塞 HTTP 请求）
        _ = Task.Run(async () =>
        {
            try
            {
                AgentSession session = await registration.Agent.CreateSessionAsync(cancellationToken);

                // 使用 AllowBackgroundResponses（需求 2.13）
                AgentRunOptions runOptions = new() { AllowBackgroundResponses = true };
                AgentResponse response = await registration.Agent.RunAsync(
                    request.Input, session, runOptions, CancellationToken.None);

                // 将结果存入会话持久化
                System.Text.Json.JsonElement serializedSession = await registration.Agent.SerializeSessionAsync(session);
                await sessionService.SaveSessionAsync(taskId, request.AgentId, serializedSession);

                logger.LogInformation("[Background] 后台任务完成 TaskId={TaskId}", taskId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Background] 后台任务失败 TaskId={TaskId}", taskId);
            }
        }, CancellationToken.None);

        return this.Accepted(new
        {
            taskId,
            status = "accepted",
            message = "Task submitted. Poll GET /api/background/{taskId} for results."
        });
    }

    /// <summary>
    /// 查询后台任务结果
    /// </summary>
    /// <param name="taskId">任务 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务状态和结果</returns>
    [HttpGet("{taskId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResultAsync(string taskId, CancellationToken cancellationToken)
    {
        System.Text.Json.JsonElement? result = await sessionService.LoadSessionAsync(taskId, cancellationToken);

        if (result is null)
        {
            return this.Accepted(new { taskId, status = "processing", message = "Task is still running." });
        }

        return this.Ok(new { taskId, status = "completed", result });
    }
}

/// <summary>
/// 后台任务请求
/// </summary>
public sealed class BackgroundTaskRequest
{
    /// <summary>
    /// 获取或设置 Agent ID
    /// </summary>
    public string AgentId { get; set; } = "writer";

    /// <summary>
    /// 获取或设置输入内容
    /// </summary>
    public string Input { get; set; } = string.Empty;
}
