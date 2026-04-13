using Inkwell.Agents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 知识库管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EditorOrAdmin")]
public sealed class KnowledgeController(KnowledgeBaseService knowledgeBase) : ControllerBase
{
    /// <summary>
    /// 获取知识库文档列表
    /// </summary>
    /// <returns>文档列表</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        IReadOnlyList<KnowledgeDocument> documents = knowledgeBase.GetAllDocuments();

        return this.Ok(documents.Select(d => new
        {
            d.Id,
            d.Title,
            d.SourceLink,
            d.AddedAt,
            contentLength = d.Content.Length
        }));
    }

    /// <summary>
    /// 上传文档到知识库
    /// </summary>
    /// <param name="request">上传请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Upload([FromBody] UploadDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return this.BadRequest("Title and Content are required.");
        }

        knowledgeBase.AddDocument(request.Title, request.Content, request.SourceLink);

        return this.Ok(new { message = "Document uploaded successfully.", title = request.Title });
    }

    /// <summary>
    /// 删除知识库文档
    /// </summary>
    /// <param name="id">文档 ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        bool removed = knowledgeBase.RemoveDocument(id);

        return removed ? this.Ok(new { message = "Document deleted." }) : this.NotFound();
    }
}

/// <summary>
/// 文档上传请求
/// </summary>
public sealed class UploadDocumentRequest
{
    /// <summary>
    /// 获取或设置文档标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文档内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置来源链接（可选）
    /// </summary>
    public string? SourceLink { get; set; }
}
