// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell.WebApi.Auth;

/// <summary>
/// 用户名密码登录请求。
/// </summary>
/// <param name="Username">用户名。</param>
/// <param name="Password">密码。</param>
public sealed record class LoginRequest(
    [Required] string Username,
    [Required] string Password);
