// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示文件内容及响应相关的元数据。
/// </summary>
/// <param name="ContentType">文件内容类型。</param>
/// <param name="CustomMetadata">自定义元数据。</param>
/// <param name="ContentDisposition">内容处置标头值。</param>
public sealed record class FileMetadata(string ContentType, IReadOnlyDictionary<string, string>? CustomMetadata = null, string? ContentDisposition = null);
