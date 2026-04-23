namespace Inkwell;

/// <summary>
/// IChatClient 的逻辑 Keyed DI 服务键。通过 <see cref="AIProviderRoutingOptions"/> 映射到物理槽位名
/// </summary>
/// <remarks>
/// 与旧的 <c>ModelServiceKeys</c> 并存；新代码推荐使用本常量
/// </remarks>
public static class AIProviderKeys
{
    /// <summary>
    /// 主模型（高质量任务：写作、审核）
    /// </summary>
    public const string Primary = "ai:primary";

    /// <summary>
    /// 辅助模型（经济任务：分析、翻译、SEO）
    /// </summary>
    public const string Secondary = "ai:secondary";

    /// <summary>
    /// 标题生成模型（低价值任务，通常绑定廉价模型）
    /// </summary>
    public const string Title = "ai:title";
}
