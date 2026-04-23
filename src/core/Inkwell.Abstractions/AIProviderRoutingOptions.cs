namespace Inkwell;

/// <summary>
/// 逻辑角色到命名槽位的路由映射。运维层可以把任意"业务逻辑角色"指向任意物理槽位而不改业务代码
/// </summary>
public sealed class AIProviderRoutingOptions
{
    /// <summary>
    /// 获取或设置 <see cref="AIProviderKeys.Primary"/> 指向的 Chat 槽位名
    /// </summary>
    public string Primary { get; set; } = "primary";

    /// <summary>
    /// 获取或设置 <see cref="AIProviderKeys.Secondary"/> 指向的 Chat 槽位名
    /// </summary>
    public string Secondary { get; set; } = "secondary";

    /// <summary>
    /// 获取或设置 <see cref="AIProviderKeys.Title"/> 指向的 Chat 槽位名（用于会话标题等低价值任务）
    /// </summary>
    public string Title { get; set; } = "secondary";

    /// <summary>
    /// 获取或设置非 Keyed 默认 Embedding 生成器指向的槽位名
    /// </summary>
    public string Embedding { get; set; } = "default";
}
