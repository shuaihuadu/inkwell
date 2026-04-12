using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// 文章评估请求
/// </summary>
public sealed class ArticleEvaluation
{
    /// <summary>
    /// 获取或设置文章标题
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文章内容
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 单篇文章评分结果
/// </summary>
[Description("文章多维度评分结果")]
public sealed class ArticleScore
{
    /// <summary>
    /// 获取或设置文章标题
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置可读性评分（1-10）
    /// </summary>
    [JsonPropertyName("readability_score")]
    public int ReadabilityScore { get; set; }

    /// <summary>
    /// 获取或设置 SEO 评分（1-10）
    /// </summary>
    [JsonPropertyName("seo_score")]
    public int SeoScore { get; set; }

    /// <summary>
    /// 获取或设置原创性评分（1-10）
    /// </summary>
    [JsonPropertyName("originality_score")]
    public int OriginalityScore { get; set; }

    /// <summary>
    /// 获取或设置综合得分
    /// </summary>
    [JsonPropertyName("total_score")]
    public double TotalScore { get; set; }

    /// <summary>
    /// 获取或设置评估反馈
    /// </summary>
    [JsonPropertyName("feedback")]
    public string Feedback { get; set; } = string.Empty;
}

/// <summary>
/// 批量评估汇总报告
/// </summary>
public sealed class BatchEvaluationReport
{
    /// <summary>
    /// 获取或设置各文章评分（按综合得分降序排列）
    /// </summary>
    [JsonPropertyName("rankings")]
    public List<ArticleScore> Rankings { get; set; } = [];

    /// <summary>
    /// 获取或设置评估总结
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}
