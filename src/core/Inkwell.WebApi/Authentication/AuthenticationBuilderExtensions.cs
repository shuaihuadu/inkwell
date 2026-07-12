// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi;

/// <summary>Inkwell 会话鉴权方案 + 授权策略的 DI 注册入口。</summary>
public static class AuthenticationBuilderExtensions
{
    /// <summary>
    /// 注册 <see cref="AuthenticationDefaults.SchemeName"/> 鉴权方案（默认方案）
    /// 以及 <see cref="AuthorizationPolicies.RequireSuperUser"/> 授权策略。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合，便于链式调用。</returns>
    public static IServiceCollection AddSessionAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddAuthentication(AuthenticationDefaults.SchemeName)
            .AddScheme<SessionAuthenticationOptions, SessionAuthenticationHandler>(AuthenticationDefaults.SchemeName, configureOptions: null);

        services
            .AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.RequireSuperUser, policy => policy.RequireClaim(SessionClaimTypes.IsSuper, "true"));

        return services;
    }
}
