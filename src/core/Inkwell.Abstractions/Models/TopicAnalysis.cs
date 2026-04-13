using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// 选题分析报告（结构化输出）
/// </summary>
[Description("选题分析报告，包含市场趋势、目标受众、关键词和热度评分")]
public sealed class TopicAnalysis
{
    /// <summary>
    /// 获取或设置原始主题
    /// </summary>
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置市场趋势分析
    /// </summary>
    [JsonPropertyName("market_trends")]
    public string MarketTrends { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置目标受众分析
    /// </summary>
    [JsonPropertyName("target_audience")]
    public string TargetAudience { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置内容角度建议
    /// </summary>
    [JsonPropertyName("content_angles")]
    public string ContentAngles { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置推荐关键词列表
    /// </summary>
    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = [];

    /// <summary>
    /// 获取或设置热度评分（1-10）
    /// </summary>
    [JsonPropertyName("popularity_score")]
    public int PopularityScore { get; set; }
}
