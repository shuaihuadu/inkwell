namespace Inkwell.WebApi;

/// <summary>Inkwell 会话鉴权方案的常量集中定义。</summary>
public static class AuthenticationDefaults
{
    /// <summary>鉴权方案名称，供 <c>AddAuthentication(...)</c> / <c>[Authorize]</c> 引用。</summary>
    public const string SchemeName = "Session";
}
