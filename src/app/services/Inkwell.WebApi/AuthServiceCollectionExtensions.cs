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

        // 始终注册授权策略（Controller 上有 [Authorize] 属性需要策略存在）
        services.AddAuthorization(options =>
        {
            if (authOptions.Enabled)
            {
                // 生产模式：要求角色
                options.AddPolicy("AdminOnly", policy => policy.RequireRole(InkwellRoles.Admin));
                options.AddPolicy("EditorOrAdmin", policy =>
                    policy.RequireRole(InkwellRoles.Admin, InkwellRoles.Editor));
                options.AddPolicy("ReviewerOrAdmin", policy =>
                    policy.RequireRole(InkwellRoles.Admin, InkwellRoles.Reviewer));
            }
            else
            {
                // 开发模式：所有策略允许匿名访问
                options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(_ => true));
                options.AddPolicy("EditorOrAdmin", policy => policy.RequireAssertion(_ => true));
                options.AddPolicy("ReviewerOrAdmin", policy => policy.RequireAssertion(_ => true));
            }
        });

        if (!authOptions.Enabled)
        {
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

        return services;
    }
}
