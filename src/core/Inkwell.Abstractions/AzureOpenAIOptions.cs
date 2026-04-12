namespace Inkwell;

/// <summary>
/// Azure OpenAI 总配置选项
/// </summary>
public sealed class AzureOpenAIOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "AzureOpenAI";

    /// <summary>
    /// 获取或设置主模型配置（写作、审核等高质量任务）
    /// </summary>
    public AzureOpenAIModelOptions Primary { get; set; } = new() { DeploymentName = "gpt-4o" };

    /// <summary>
    /// 获取或设置辅助模型配置（分析、翻译等经济任务）
    /// </summary>
    public AzureOpenAIModelOptions Secondary { get; set; } = new() { DeploymentName = "gpt-4o-mini" };

    /// <summary>
    /// 获取或设置嵌入模型配置（记忆和 RAG 场景）
    /// </summary>
    public AzureOpenAIModelOptions Embedding { get; set; } = new() { DeploymentName = "text-embedding-3-small" };
}

/// <summary>
/// 单个 Azure OpenAI 模型的连接配置
/// </summary>
public sealed class AzureOpenAIModelOptions
{
    /// <summary>
    /// 获取或设置 Azure OpenAI 服务端点
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 API 密钥。为空时自动回退到 AzureCliCredential
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置模型部署名称
    /// </summary>
    public string DeploymentName { get; set; } = string.Empty;
}
