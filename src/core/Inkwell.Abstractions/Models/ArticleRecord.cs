namespace Inkwell;

/// <summary>
/// 文章持久化记录（数据载体）
/// </summary>
public sealed class ArticleRecord
{
    /// <summary>
    /// 获取或设置文章唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

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
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置修订版本号
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// 获取或设置创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 获取或设置更新时间
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
