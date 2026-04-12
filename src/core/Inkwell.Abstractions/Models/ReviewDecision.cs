using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// 审核决策
/// </summary>
public sealed class ReviewDecision
{
    /// <summary>
    /// 获取或设置是否通过
    /// </summary>
    [JsonPropertyName("approved")]
    public bool Approved { get; set; }

    /// <summary>
    /// 获取或设置审核反馈
    /// </summary>
    [JsonPropertyName("feedback")]
    public string Feedback { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置评分（1-10）
    /// </summary>
    [JsonPropertyName("score")]
    public int Score { get; set; }
}
