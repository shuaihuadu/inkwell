using Inkwell;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 认证控制器（开发用 Token 生成端点）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(TokenService tokenService) : ControllerBase
{
    /// <summary>
    /// 生成 JWT Token（开发环境用，生产环境应对接外部 IdP）
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <returns>JWT Token</returns>
    [HttpPost("token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GenerateToken([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return this.BadRequest("Username is required.");
        }

        // 开发环境：根据用户名前缀分配角色
        string role = request.Username.ToLowerInvariant() switch
        {
            string u when u.StartsWith("admin") => InkwellRoles.Admin,
            string u when u.StartsWith("reviewer") => InkwellRoles.Reviewer,
            _ => InkwellRoles.Editor
        };

        string token = tokenService.GenerateToken(request.Username, role);

        return this.Ok(new
        {
            token,
            username = request.Username,
            role,
            expiresIn = "24h"
        });
    }
}

/// <summary>
/// 登录请求
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// 获取或设置用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
