// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

public sealed record class FileUploadResult(string Container, string Key, long SizeBytes, string ETag, DateTimeOffset UploadedTime);
