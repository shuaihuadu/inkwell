using System.Text.Json.Serialization;

namespace Inkwell.Core;

/// <summary>
/// 选题分析报告
/// </summary>
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
}
