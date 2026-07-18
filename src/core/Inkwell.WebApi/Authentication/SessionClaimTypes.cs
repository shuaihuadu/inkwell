// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi;

/// <summary>
/// Inkwell 自定义 Claim 类型。标准 <see cref="System.Security.Claims.ClaimTypes"/> 未提供
/// <see cref="AuthSession.IsAdmin"/> 对应的项，因此单独定义。
/// </summary>
public static class SessionClaimTypes
{
    /// <summary>对应 <see cref="AuthSession.IsAdmin"/>；取值固定为 <c>"true"</c>/<c>"false"</c> 字符串。</summary>
    public const string IsAdmin = "inkwell:is_admin";

    /// <summary>对应 <see cref="AuthSession.MustChangePassword"/>；取值固定为 <c>"true"</c>/<c>"false"</c> 字符串。</summary>
    public const string MustChangePassword = "inkwell:must_change_password";
}
