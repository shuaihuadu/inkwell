// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示文件存储中的对象信息。
/// </summary>
/// <param name="Container">容器名称。</param>
/// <param name="Key">对象键。</param>
/// <param name="SizeBytes">对象大小（字节）。</param>
/// <param name="ETag">对象实体标记。</param>
/// <param name="LastModifiedTime">对象最后修改时间。</param>
/// <param name="ContentType">对象内容类型。</param>
public sealed record class FileObjectInfo(string Container, string Key, long SizeBytes, string ETag, DateTimeOffset LastModifiedTime, string? ContentType);
