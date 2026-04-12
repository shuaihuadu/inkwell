namespace Inkwell;

/// <summary>
/// 授权配置选项
/// </summary>
public sealed class AuthOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Auth";

    /// <summary>
    /// 获取或设置是否启用认证。开发环境可设为 false 跳过认证
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 获取或设置 JWT 签名密钥
    /// </summary>
    public string SecretKey { get; set; } = "inkwell-dev-secret-key-at-least-32-characters-long!!";

    /// <summary>
    /// 获取或设置 JWT 签发者
    /// </summary>
    public string Issuer { get; set; } = "Inkwell";

    /// <summary>
    /// 获取或设置 JWT 受众
    /// </summary>
    public string Audience { get; set; } = "Inkwell.WebApi";

    /// <summary>
    /// 获取或设置 Token 过期时间（小时）
    /// </summary>
    public int TokenExpirationHours { get; set; } = 24;
}

/// <summary>
/// Inkwell 角色常量
/// </summary>
public static class InkwellRoles
{
    /// <summary>
    /// 管理员：全部权限
    /// </summary>
    public const string Admin = "admin";

    /// <summary>
    /// 编辑：Agent 对话、Workflow 触发、文章管理
    /// </summary>
    public const string Editor = "editor";

    /// <summary>
    /// 审核员：仅人工审核操作
    /// </summary>
    public const string Reviewer = "reviewer";
}
