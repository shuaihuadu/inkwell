// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示文件上传结果。
/// </summary>
/// <param name="Container">容器名称。</param>
/// <param name="Key">对象键。</param>
/// <param name="SizeBytes">文件大小（字节）。</param>
/// <param name="ETag">文件实体标记。</param>
/// <param name="UploadedTime">文件上传时间。</param>
public sealed record class FileUploadResult(string Container, string Key, long SizeBytes, string ETag, DateTimeOffset UploadedTime);
