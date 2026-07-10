using Microsoft.AspNetCore.Authentication;

namespace Inkwell.WebApi;

/// <summary>
/// <see cref="AuthenticationDefaults.SchemeName"/> 方案的选项。当前无需额外可配置项，
/// 仅作为 <see cref="AuthenticationSchemeOptions"/> 的具体类型占位（<c>AddScheme&lt;TOptions, THandler&gt;</c> 强制要求）。
/// </summary>
public sealed class SessionAuthenticationOptions : AuthenticationSchemeOptions;
