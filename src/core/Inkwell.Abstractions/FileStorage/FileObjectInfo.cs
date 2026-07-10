// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

public sealed record class FileObjectInfo(string Container, string Key, long SizeBytes, string ETag, DateTimeOffset LastModifiedTime, string? ContentType);
