// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="IAuthService"/> 的 DI 注册入口。</summary>
public static class AuthBuilderExtensions
{
    /// <summary>
    /// 注册默认身份验证服务。
    /// </summary>
    /// <param name="builder">Inkwell 构建器。</param>
    /// <returns>当前 Inkwell 构建器。</returns>
    public static IInkwellBuilder UseDefaultAuthService(this IInkwellBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>());
        builder.Services.AddOptions<AuthOptions>().Bind(builder.Configuration.GetSection("Inkwell:Auth"));

        return builder;
    }
}
