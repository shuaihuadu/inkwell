namespace Inkwell;

/// <summary>
/// IChatClient 的 Keyed DI 服务键常量（旧版）
/// </summary>
/// <remarks>
/// 已由 <see cref="AIProviderKeys"/> 取代；新的注册入口 <c>UseAIProviders</c> 仍会同时以本类定义的键名注册一份以保证向后兼容。
/// 新代码请直接引用 <see cref="AIProviderKeys"/>
/// </remarks>
[Obsolete("Use AIProviderKeys instead. This type is retained only for backward compatibility and may be removed in a future major version.")]
public static class ModelServiceKeys
{
    /// <summary>
    /// 主模型（高质量任务：写作、审核）
    /// </summary>
    public const string Primary = "Primary";

    /// <summary>
    /// 辅助模型（经济任务：分析、翻译、SEO）
    /// </summary>
    public const string Secondary = "Secondary";

    /// <summary>
    /// 嵌入模型（记忆、RAG）
    /// </summary>
    public const string Embedding = "Embedding";
}
