using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// SEO 分析报告（结构化输出）
/// </summary>
[Description("SEO 分析报告，包含标题建议、关键词密度、元描述和优化建议")]
public sealed class SeoReport
{
    /// <summary>
    /// 获取或设置原始标题
    /// </summary>
    [JsonPropertyName("original_title")]
    public string OriginalTitle { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置优化后的标题建议
    /// </summary>
    [JsonPropertyName("suggested_title")]
    public string SuggestedTitle { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置推荐的元描述
    /// </summary>
    [JsonPropertyName("meta_description")]
    public string MetaDescription { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置主关键词
    /// </summary>
    [JsonPropertyName("primary_keyword")]
    public string PrimaryKeyword { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置次要关键词列表
    /// </summary>
    [JsonPropertyName("secondary_keywords")]
    public List<string> SecondaryKeywords { get; set; } = [];

    /// <summary>
    /// 获取或设置关键词密度百分比
    /// </summary>
    [JsonPropertyName("keyword_density")]
    public double KeywordDensity { get; set; }

    /// <summary>
    /// 获取或设置 SEO 综合评分（1-100）
    /// </summary>
    [JsonPropertyName("seo_score")]
    public int SeoScore { get; set; }

    /// <summary>
    /// 获取或设置优化建议列表
    /// </summary>
    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; set; } = [];
}
