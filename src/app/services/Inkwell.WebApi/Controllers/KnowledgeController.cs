using Inkwell.Agents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 知识库管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = InkwellPolicies.EditorOrAdmin)]
public sealed class KnowledgeController(
    KnowledgeBaseService knowledgeBase,
    KnowledgePersistenceService persistence,
    ILogger<KnowledgeController> logger) : ControllerBase
{
    /// <summary>
    /// 允许上传的文件扩展名
    /// </summary>
    private static readonly HashSet<string> s_allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".text", ".md", ".markdown"
    };

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
            d.FileType,
            d.SourceLink,
            d.ChunkCount,
            d.AddedAt,
            contentLength = d.Content.Length
        }));
    }

    /// <summary>
    /// 上传文档到知识库（JSON body）
    /// </summary>
    /// <param name="request">上传请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAsync(
        [FromBody] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return this.BadRequest("Title and Content are required.");
        }

        string id = await knowledgeBase.AddDocumentAsync(
            request.Title,
            request.Content,
            sourceLink: request.SourceLink,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // 持久化到 DB
        await persistence.SaveDocumentAsync(
            id, request.Title, request.Content, "txt", request.SourceLink,
            knowledgeBase.GetChunks(id), cancellationToken).ConfigureAwait(false);

        logger.LogInformation("[Knowledge] Uploaded document {Id}: {Title}", id, request.Title);

        return this.Ok(new { id, message = "Document uploaded successfully.", title = request.Title });
    }

    /// <summary>
    /// 上传文件到知识库（支持 .txt / .md）
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("upload-file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFileAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return this.BadRequest("File is required.");
        }

        string ext = Path.GetExtension(file.FileName);
        if (!s_allowedExtensions.Contains(ext))
        {
            return this.BadRequest($"Unsupported file type: {ext}. Allowed: {string.Join(", ", s_allowedExtensions)}");
        }

        // 限制文件大小（5 MB）
        if (file.Length > 5 * 1024 * 1024)
        {
            return this.BadRequest("File size exceeds 5 MB limit.");
        }

        using StreamReader reader = new(file.OpenReadStream());
        string content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(content))
        {
            return this.BadRequest("File content is empty.");
        }

        string id = await knowledgeBase.AddFromFileAsync(
            file.FileName,
            content,
            cancellationToken).ConfigureAwait(false);

        // 持久化到 DB
        string fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
        string fileType = fileExt is ".md" or ".markdown" ? "md" : "txt";
        await persistence.SaveDocumentAsync(
            id, Path.GetFileNameWithoutExtension(file.FileName), content, fileType,
            $"file://{file.FileName}", knowledgeBase.GetChunks(id), cancellationToken).ConfigureAwait(false);

        logger.LogInformation("[Knowledge] Uploaded file {Id}: {FileName} ({Size} bytes)",
            id, file.FileName, file.Length);

        return this.Ok(new
        {
            id,
            message = "File uploaded successfully.",
            fileName = file.FileName,
            contentLength = content.Length,
            chunkCount = knowledgeBase.GetChunks(id).Count
        });
    }

    /// <summary>
    /// 获取文档的切片列表
    /// </summary>
    /// <param name="id">文档 ID</param>
    /// <returns>切片列表</returns>
    [HttpGet("{id}/chunks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetChunks(string id)
    {
        IReadOnlyList<KnowledgeChunk> chunks = knowledgeBase.GetChunks(id);

        if (chunks.Count == 0)
        {
            return this.NotFound();
        }

        return this.Ok(chunks.Select(c => new
        {
            c.Id,
            c.ChunkIndex,
            contentLength = c.Content.Length,
            preview = c.Content[..Math.Min(c.Content.Length, 200)]
        }));
    }

    /// <summary>
    /// 删除知识库文档
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        bool removed = knowledgeBase.RemoveDocument(id);

        if (!removed)
        {
            return this.NotFound();
        }

        // 从 DB 删除
        await persistence.DeleteDocumentAsync(id, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("[Knowledge] Deleted document {Id}", id);

        return this.Ok(new { message = "Document deleted." });
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
    /// 获取或设置来源链接
    /// </summary>
    public string? SourceLink { get; set; }
}
