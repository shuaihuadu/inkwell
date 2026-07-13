// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi;

/// <summary>Inkwell 授权策略名称的常量集中定义。</summary>
public static class AuthorizationPolicies
{
    /// <summary>要求请求携带有效的 Inkwell 会话令牌。</summary>
    public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";

    /// <summary>要求 <see cref="SessionClaimTypes.IsSuper"/> 为 <c>true</c>（超级管理员）。</summary>
    public const string RequireSuperUser = "RequireSuperUser";
}
