namespace Inkwell;

/// <summary>
/// 文章实体
/// </summary>
public sealed class Article
{
    /// <summary>
    /// 获取或设置文章唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 获取或设置文章主题
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文章标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文章正文
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文章状态
    /// </summary>
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    /// <summary>
    /// 获取或设置创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 获取或设置修订版本号
    /// </summary>
    public int Revision { get; set; }
}
