namespace Inkwell;

/// <summary>
/// 多 AI Provider 聚合配置（对应 appsettings 的 "AIProviders" 节）
/// </summary>
public sealed class AIProviderOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "AIProviders";

    /// <summary>
    /// 获取或设置 Chat 模型命名槽位字典。键是槽位名（如 primary / secondary / creative / local / deepseek），值是端点配置
    /// </summary>
    public Dictionary<string, AIEndpointOptions> Chat { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取或设置 Embedding 模型命名槽位字典
    /// </summary>
    public Dictionary<string, AIEndpointOptions> Embedding { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取或设置逻辑角色到命名槽位的路由映射
    /// </summary>
    public AIProviderRoutingOptions Routing { get; set; } = new();
}
