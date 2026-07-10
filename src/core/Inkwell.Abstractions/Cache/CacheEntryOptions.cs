// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// <see cref="ICacheProvider.SetAsync{T}"/> 强制 TTL 载体，禁止无过期时间的缓存写入。
/// </summary>
public sealed record class CacheEntryOptions(TimeSpan AbsoluteExpirationRelativeToNow);
