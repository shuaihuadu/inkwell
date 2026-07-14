// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell.WebApi.Auth;

/// <summary>
/// 客户端锁定后的密码再验证请求。
/// </summary>
/// <param name="Password">当前用户密码。</param>
public sealed record class UnlockRequest([Required, StringLength(256)] string Password);