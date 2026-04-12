using System.Text;
using Inkwell;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Inkwell.WebApi;

/// <summary>
/// 授权与认证服务注册扩展方法
/// </summary>
public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// 注册 JWT 认证与授权服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddInkwellAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // 绑定配置
        IConfigurationSection section = configuration.GetSection(AuthOptions.SectionName);
        services.Configure<AuthOptions>(section);

        AuthOptions authOptions = new();
        section.Bind(authOptions);

        // 注册 Token 生成服务
        services.AddSingleton<TokenService>();

        if (!authOptions.Enabled)
        {
            // 认证未启用时，仍注册 Authentication 但不强制要求
            return services;
        }

        // 配置 JWT Bearer 认证
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = authOptions.Issuer,
                    ValidAudience = authOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SecretKey))
                };
            });

        services.AddAuthorization(options =>
        {
            // admin：全部权限
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(InkwellRoles.Admin));

            // editor：Agent 对话、Workflow 触发、文章管理
            options.AddPolicy("EditorOrAdmin", policy =>
                policy.RequireRole(InkwellRoles.Admin, InkwellRoles.Editor));

            // reviewer：人工审核
            options.AddPolicy("ReviewerOrAdmin", policy =>
                policy.RequireRole(InkwellRoles.Admin, InkwellRoles.Reviewer));
        });

        return services;
    }
}
