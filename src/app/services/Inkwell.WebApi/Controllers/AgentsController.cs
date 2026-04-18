using Inkwell.Agents;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// Agent 管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = InkwellPolicies.EditorOrAdmin)]
public sealed class AgentsController(AgentRegistry agentRegistry, ILogger<AgentsController> logger) : ControllerBase
{
    /// <summary>
    /// 获取所有已注册的 Agent
    /// </summary>
    /// <returns>Agent 列表</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        IReadOnlyList<AgentRegistration> agents = agentRegistry.GetAll();

        var result = agents.Select(a => new
        {
            a.Id,
            a.Name,
            a.Description,
            a.AguiRoute
        });

        return this.Ok(result);
    }

    /// <summary>
    /// 根据 ID 获取 Agent 信息
    /// </summary>
    /// <param name="id">Agent ID</param>
    /// <returns>Agent 信息</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(string id)
    {
        AgentRegistration? agent = agentRegistry.GetById(id);

        if (agent is null)
        {
            return this.NotFound();
        }

        return this.Ok(new
        {
            agent.Id,
            agent.Name,
            agent.Description,
            agent.AguiRoute
        });
    }

    /// <summary>
    /// 使用 Image Analyst Agent 分析上传的图片（多模态 2.9）
    /// </summary>
    /// <param name="file">上传的图片文件</param>
    /// <param name="prompt">分析提示（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>图片分析结果</returns>
    [HttpPost("image-analyst/analyze")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeImageAsync(
        IFormFile file,
        [FromForm] string? prompt,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return this.BadRequest("Image file is required.");
        }

        // 验证文件类型
        string[] allowedTypes = ["image/png", "image/jpeg", "image/webp", "image/gif"];
        if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return this.BadRequest($"Unsupported image type: {file.ContentType}. Allowed: {string.Join(", ", allowedTypes)}");
        }

        AgentRegistration? registration = agentRegistry.GetById("image-analyst");
        if (registration is null)
        {
            return this.NotFound("Image Analyst Agent not found.");
        }

        logger.LogInformation("[ImageAnalyst] Analyzing image: {FileName}, Size: {Size}KB",
            file.FileName, file.Length / 1024);

        // 读取图片为 byte[]，构造多模态消息
        using MemoryStream memoryStream = new();
        await file.CopyToAsync(memoryStream, cancellationToken);
        byte[] imageBytes = memoryStream.ToArray();

        // 构造多模态消息：文本 + 图片
        string textPrompt = prompt ?? "请分析这张图片，提供：1) 图片描述 2) ALT 标签 3) 配图说明 4) 标签建议";

        ChatMessage message = new(ChatRole.User,
        [
            new TextContent(textPrompt),
            new DataContent(imageBytes, file.ContentType)
        ]);

        // 调用 Agent
        AgentSession session = await registration.Agent.CreateSessionAsync(cancellationToken);
        AgentResponse response = await registration.Agent.RunAsync(message, session, cancellationToken: cancellationToken);

        logger.LogInformation("[ImageAnalyst] Analysis completed for: {FileName}", file.FileName);

        return this.Ok(new
        {
            fileName = file.FileName,
            contentType = file.ContentType,
            sizeKB = file.Length / 1024,
            analysis = response.Text
        });
    }
}
