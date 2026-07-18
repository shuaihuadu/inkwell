// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Auth;

/// <summary>修改当前用户密码请求。</summary>
/// <param name="CurrentPassword">当前密码。</param>
/// <param name="NewPassword">新密码。</param>
public sealed record class ChangePasswordRequest(string CurrentPassword, string NewPassword);
