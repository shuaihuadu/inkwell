using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary><see cref="IAuthService"/> 的 DI 注册入口。</summary>
public static class AuthBuilderExtensions
{
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
