using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;

namespace Inkwell.AI.AzureOpenAI;

/// <summary>
/// Azure OpenAI 客户端构造工厂，统一处理凭据回退逻辑
/// </summary>
internal static class AzureOpenAIClientFactory
{
    /// <summary>
    /// 根据端点配置创建 <see cref="AzureOpenAIClient"/>。ApiKey 为空时回退到 <see cref="AzureCliCredential"/>
    /// </summary>
    /// <param name="options">端点配置</param>
    /// <returns>Azure OpenAI 客户端</returns>
    public static AzureOpenAIClient Create(AIEndpointOptions options)
    {
        Uri endpoint = new(options.Endpoint!);

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return new AzureOpenAIClient(endpoint, new AzureKeyCredential(options.ApiKey));
        }

        return new AzureOpenAIClient(endpoint, new AzureCliCredential());
    }
}
