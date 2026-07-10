using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Inkwell.WebApi;

/// <summary>
/// Inkwell 会话鉴权方案的处理器：从 <c>Authorization: Bearer &lt;SessionToken&gt;</c> 请求头取出
/// <see cref="IAuthService"/> 签发的不透明会话 token，校验后构造 <see cref="ClaimsPrincipal"/>。
/// </summary>
/// <remarks>
/// 客户端是 Electron 桌面应用（非浏览器页面），采用 Bearer Header 而非 Cookie，
/// 天然规避 CSRF 场景，无需额外的 <c>SameSite</c>/防伪 token 处理。
/// </remarks>
internal sealed class SessionAuthenticationHandler(
    IOptionsMonitor<SessionAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAuthService authService) : AuthenticationHandler<SessionAuthenticationOptions>(options, logger, encoder)
{
    private const string BearerPrefix = "Bearer ";

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!this.Request.Headers.TryGetValue("Authorization", out StringValues authorizationHeader))
        {
            return AuthenticateResult.NoResult();
        }

        string? headerValue = authorizationHeader.ToString();

        if (string.IsNullOrEmpty(headerValue) || !headerValue.StartsWith(BearerPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        string sessionToken = headerValue[BearerPrefix.Length..].Trim();

        if (string.IsNullOrEmpty(sessionToken))
        {
            return AuthenticateResult.NoResult();
        }

        AuthSession session;

        try
        {
            session = await authService.ValidateSessionAsync(sessionToken, this.Context.RequestAborted).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthenticateResult.Fail(ex);
        }

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new Claim(ClaimTypes.Name, session.Username),
            new Claim(SessionClaimTypes.IsSuper, session.IsSuper ? "true" : "false"),
        ];

        ClaimsIdentity identity = new(claims, this.Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, this.Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
