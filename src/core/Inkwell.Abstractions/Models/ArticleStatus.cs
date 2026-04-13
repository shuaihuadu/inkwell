namespace Inkwell;

/// <summary>
/// 文章状态
/// </summary>
public enum ArticleStatus
{
    /// <summary>
    /// 草稿
    /// </summary>
    Draft,

    /// <summary>
    /// 审核中
    /// </summary>
    InReview,

    /// <summary>
    /// 审核通过
    /// </summary>
    Approved,

    /// <summary>
    /// 被退回
    /// </summary>
    Rejected,

    /// <summary>
    /// 已发布
    /// </summary>
    Published
}
