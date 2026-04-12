namespace Inkwell;

/// <summary>
/// Azure OpenAI 连接配置选项
/// </summary>
public sealed class AzureOpenAIOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "AzureOpenAI";

    /// <summary>
    /// 获取或设置 Azure OpenAI 服务端点
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 API 密钥。为空时自动回退到 AzureCliCredential
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置主模型部署名称（用于写作、审核等高质量任务）
    /// </summary>
    public string PrimaryDeploymentName { get; set; } = "gpt-4o";

    /// <summary>
    /// 获取或设置辅助模型部署名称（用于分析、翻译等经济任务）
    /// </summary>
    public string SecondaryDeploymentName { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// 获取或设置嵌入模型部署名称（用于记忆和 RAG 场景）
    /// </summary>
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-small";
}
