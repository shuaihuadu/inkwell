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
/// 单个模型的连接配置（支持 Azure OpenAI 与 OpenAI 兼容协议两种 Provider）
/// </summary>
public sealed class AzureOpenAIModelOptions
{
    /// <summary>
    /// 获取或设置 Provider 类型。取值见 <see cref="ModelProviderTypes"/>，默认为 AzureOpenAI
    /// </summary>
    /// <remarks>
    /// - AzureOpenAI：走 Azure.AI.OpenAI SDK，使用 Endpoint + DeploymentName + AzureCliCredential 或 ApiKey<br/>
    /// - OpenAICompatible：走 OpenAI SDK，使用 Endpoint 作为 BaseUrl + DeploymentName 作为模型名 + ApiKey（必填），
    ///   适用于 DeepSeek、Qwen、Moonshot、智谱 GLM、OpenAI 官方等任何实现 OpenAI 兼容协议的端点
    /// </remarks>
    public string Provider { get; set; } = ModelProviderTypes.AzureOpenAI;

    /// <summary>
    /// 获取或设置服务端点。
    /// AzureOpenAI 模式下为 Azure 资源 Endpoint；OpenAICompatible 模式下为 BaseUrl（如 https://api.deepseek.com/v1）
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 API 密钥。
    /// AzureOpenAI 模式下为空时自动回退到 AzureCliCredential；OpenAICompatible 模式下必填
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置模型标识。
    /// AzureOpenAI 模式下为 Deployment 名称；OpenAICompatible 模式下为模型名（如 deepseek-chat、qwen-max）
    /// </summary>
    public string DeploymentName { get; set; } = string.Empty;
}

/// <summary>
/// 模型 Provider 类型常量
/// </summary>
public static class ModelProviderTypes
{
    /// <summary>
    /// Azure OpenAI（默认）
    /// </summary>
    public const string AzureOpenAI = "AzureOpenAI";

    /// <summary>
    /// OpenAI 兼容协议（OpenAI 官方、DeepSeek、Qwen、Moonshot、智谱 GLM 等）
    /// </summary>
    public const string OpenAICompatible = "OpenAICompatible";
}
