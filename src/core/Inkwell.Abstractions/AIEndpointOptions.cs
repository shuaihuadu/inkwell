namespace Inkwell;

/// <summary>
/// 单个 AI 模型端点的连接配置
/// </summary>
/// <remarks>
/// 每种 Provider 只消费自己关心的字段，语义由对应 <see cref="IAIChatProvider"/> 或 <see cref="IAIEmbeddingProvider"/> 决定
/// </remarks>
public sealed class AIEndpointOptions
{
    /// <summary>
    /// 获取或设置 Provider 类型标识，用于匹配 <see cref="IAIChatProvider.Name"/>。示例：AzureOpenAI、OpenAI、OpenAICompatible、Ollama、Anthropic
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置服务端点（Azure Endpoint / Ollama URL 等）
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// 获取或设置 OpenAI 兼容协议的基地址（DeepSeek / Qwen / Moonshot 等）
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// 获取或设置 Azure 专用的 Deployment 名称
    /// </summary>
    public string? Deployment { get; set; }

    /// <summary>
    /// 获取或设置非 Azure 场景下的模型名（deepseek-chat / qwen-max / gpt-4o 等）
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 获取或设置 API 密钥。空值的语义由各 Provider 自行决定
    /// </summary>
    /// <remarks>
    /// Azure 空值走 DefaultAzureCredential / AzureCliCredential；OpenAI 兼容空值通常表示读环境变量；Ollama 不需要 ApiKey
    /// </remarks>
    public string? ApiKey { get; set; }

    /// <summary>
    /// 获取或设置 Provider 私有的扩展参数（如 API 版本、组织 ID、代理等）
    /// </summary>
    public Dictionary<string, string> Extras { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
