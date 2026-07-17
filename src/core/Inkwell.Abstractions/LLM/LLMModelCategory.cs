// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// 定义 Inkwell 支持的模型产品分类。
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LLMModelCategory
{
    /// <summary>
    /// 未知或暂不支持的模型分类。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 对话与文本生成模型。
    /// </summary>
    Chat = 1,

    /// <summary>
    /// 文本嵌入模型。
    /// </summary>
    Embedding = 2,

    /// <summary>
    /// 图片生成或编辑模型。
    /// </summary>
    ImageGeneration = 3,

    /// <summary>
    /// 视频生成模型。
    /// </summary>
    VideoGeneration = 4,
}