using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Inkwell.Workflows.Tools;

/// <summary>
/// 工作流专用的文章工具集合
/// </summary>
/// <remarks>
/// <para>
/// 与 <see cref="Inkwell.Agents.InkwellTools"/> 里纯演示字符串版本不同，
/// 这里的工具会通过 <see cref="ArticleWriteGateway"/> 真正把文章写入持久化层，
/// 供内容生产流水线 <c>PublisherAgent</c> 作为 AIFunction 调用。
/// </para>
/// <para>
/// 网关在宿主 DI 构建完成后才具备 <c>ScopeFactory</c>；本类是有状态的实例类，
/// 由 <see cref="ContentPipelineBuilder"/> 在构建工作流时与网关一并注入到 Agent。
/// </para>
/// </remarks>
public sealed class ArticleWorkflowTools(ArticleWriteGateway gateway, ILogger<ArticleWorkflowTools>? logger = null)
{
    private readonly ArticleWriteGateway _gateway = gateway;
    private readonly ILogger<ArticleWorkflowTools>? _logger = logger;

    /// <summary>
    /// 真正把一篇已审核通过的文章发布到内容库
    /// </summary>
    /// <param name="topic">文章主题</param>
    /// <param name="title">文章标题</param>
    /// <param name="content">文章正文（完整 Markdown）</param>
    /// <param name="revision">当前修订版本号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新发布文章的 ID</returns>
    [Description("将一篇已通过人工审核的文章写入内容库并置为 Published。参数需要完整提供；调用成功会返回新文章 Id，调用后不要再做其他输出。")]
    public async Task<string> PublishArticleAsync(
        [Description("文章主题")] string topic,
        [Description("文章标题")] string title,
        [Description("完整正文，Markdown 格式")] string content,
        [Description("当前修订版本号，从审核上下文中取得")] int revision,
        CancellationToken cancellationToken = default)
    {
        ArticleRecord record = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Topic = topic,
            Title = title,
            Content = content,
            Status = nameof(ArticleStatus.Published),
            Revision = revision,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await this._gateway.AddAsync(record, this._logger, cancellationToken).ConfigureAwait(false);
        this._logger?.LogInformation("[ArticleWorkflowTools] publish_article invoked. Id={Id} Title={Title}", record.Id, record.Title);
        return record.Id;
    }
}
