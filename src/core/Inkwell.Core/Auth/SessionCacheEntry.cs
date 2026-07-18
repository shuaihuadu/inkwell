// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><c>ICacheProvider</c> 序列化载体；内部实现细节，不对外暴露。</summary>
internal sealed record class SessionCacheEntry(Guid UserId, string Username, bool IsAdmin, bool MustChangePassword, int SessionVersion, DateTimeOffset IssuedAt);
