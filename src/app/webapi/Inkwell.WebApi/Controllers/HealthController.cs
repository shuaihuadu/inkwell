using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 健康检查控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// 获取服务健康状态
    /// </summary>
    /// <returns>健康状态信息</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return this.Ok(new { status = "healthy", version = "0.1.0" });
    }
}
