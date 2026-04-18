using Inkwell.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 人工审核（HITL）决策端点
/// Workflow 在命中 RequestInfoEvent 时会把挂起项注册到 <see cref="HitlPendingRegistry"/>，
/// 前端收到 HITL 标记后通过本端点回写决策（通过/退回），驱动 Workflow 继续执行
/// </summary>
[ApiController]
[Route("api/hitl")]
[Authorize(Policy = InkwellPolicies.EditorOrAdmin)]
public sealed class HitlController(
    HitlPendingRegistry registry,
    ILogger<HitlController> logger) : ControllerBase
{
    /// <summary>
    /// 回写人工审核决策
    /// </summary>
    /// <param name="requestId">Workflow 挂起时分配的 RequestId</param>
    /// <param name="dto">决策内容</param>
    /// <returns>Http 响应</returns>
    [HttpPost("{requestId}/respond")]
    public async Task<IActionResult> Respond(string requestId, [FromBody] HitlResponseDto dto)
    {
        HitlPendingEntry? entry = registry.TakeOut(requestId);
        if (entry is null)
        {
            logger.LogWarning("[HITL] Request not found or already responded. RequestId={RequestId}", requestId);
            return this.NotFound(new { message = "审核请求不存在或已处理" });
        }

        try
        {
            ExternalResponse response = entry.Event.Request.CreateResponse(dto.Approved);
            await entry.Run.SendResponseAsync(response).ConfigureAwait(false);

            logger.LogInformation("[HITL] Response sent. RequestId={RequestId} Approved={Approved}", requestId, dto.Approved);
            return this.Ok(new { requestId, approved = dto.Approved });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[HITL] Failed to send response. RequestId={RequestId}", requestId);
            return this.StatusCode(500, new { message = "回写决策失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前挂起的 HITL 数量（调试 / 监控用途）
    /// </summary>
    /// <returns>挂起数量</returns>
    [HttpGet("pending-count")]
    public IActionResult PendingCount() => this.Ok(new { count = registry.Count });
}

/// <summary>
/// 人工审核决策 DTO
/// </summary>
public sealed class HitlResponseDto
{
    /// <summary>
    /// 获取或设置是否通过（true=发布，false=退回修改）
    /// </summary>
    public bool Approved { get; set; }

    /// <summary>
    /// 获取或设置审核备注（可选，当前未透传给 Workflow）
    /// </summary>
    public string? Feedback { get; set; }
}
