namespace Inkwell;

/// <summary>
/// IChatClient 的 Keyed DI 服务键常量
/// </summary>
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
