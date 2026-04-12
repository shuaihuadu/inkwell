using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// 翻译结果
/// </summary>
public sealed class TranslationResult
{
    /// <summary>
    /// 获取或设置目标语言
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置翻译后的内容
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 多语言翻译聚合结果
/// </summary>
public sealed class MultiLanguageResult
{
    /// <summary>
    /// 获取或设置原文
    /// </summary>
    [JsonPropertyName("original")]
    public string Original { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置各语言翻译结果
    /// </summary>
    [JsonPropertyName("translations")]
    public List<TranslationResult> Translations { get; set; } = [];
}
