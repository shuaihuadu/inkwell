using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// Azure OpenAI Agent Runtime 凭证配置；不进 <c>Inkwell.Abstractions</c>（端口零外部包约束）。
/// 从 <c>appsettings.json</c> <c>"Inkwell:AgentRuntime:AzureOpenAI"</c> 段绑定。
/// </summary>
public sealed class AzureOpenAIAgentRuntimeOptions
{
    [Required]
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>为空时应改用受管身份等其他鉴权方式（v1 暂只支持 API Key）。</summary>
    public string? ApiKey { get; init; }

    [Required]
    public string DeploymentName { get; init; } = string.Empty;
}
