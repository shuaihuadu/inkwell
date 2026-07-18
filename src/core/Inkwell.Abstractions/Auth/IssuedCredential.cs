// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>仅显示一次的账号临时凭据。</summary>
/// <param name="UserId">用户标识。</param>
/// <param name="Username">用户名。</param>
/// <param name="TemporaryPassword">一次性临时密码。</param>
public sealed record class IssuedCredential(Guid UserId, string Username, string TemporaryPassword);
