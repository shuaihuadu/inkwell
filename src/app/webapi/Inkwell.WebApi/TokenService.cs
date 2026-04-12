using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Inkwell.WebApi;

/// <summary>
/// JWT Token 生成服务（开发用，生产环境应使用 KeyCloak 等外部 Identity Provider）
/// </summary>
public sealed class TokenService(IOptions<AuthOptions> options)
{
    private readonly AuthOptions _options = options.Value;

    /// <summary>
    /// 为指定用户生成 JWT Token
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="role">角色</param>
    /// <returns>JWT Token 字符串</returns>
    public string GenerateToken(string username, string role)
    {
        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(this._options.SecretKey));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        JwtSecurityToken token = new(
            issuer: this._options.Issuer,
            audience: this._options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(this._options.TokenExpirationHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
