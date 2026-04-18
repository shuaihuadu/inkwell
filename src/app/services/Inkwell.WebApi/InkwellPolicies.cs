namespace Inkwell.WebApi;

/// <summary>
/// Inkwell 授权策略名称常量
/// </summary>
public static class InkwellPolicies
{
    /// <summary>
    /// 仅 Admin 角色
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Editor 或 Admin 角色
    /// </summary>
    public const string EditorOrAdmin = "EditorOrAdmin";

    /// <summary>
    /// Reviewer 或 Admin 角色
    /// </summary>
    public const string ReviewerOrAdmin = "ReviewerOrAdmin";
}
