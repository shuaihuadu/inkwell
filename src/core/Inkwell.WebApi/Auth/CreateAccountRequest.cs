// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Auth;

/// <summary>管理员创建账号请求。</summary>
/// <param name="Username">用户名。</param>
/// <param name="IsAdmin">是否授予管理员角色。</param>
public sealed record class CreateAccountRequest(string Username, bool IsAdmin);
