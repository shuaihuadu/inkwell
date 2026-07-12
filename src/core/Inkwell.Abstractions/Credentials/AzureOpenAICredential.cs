// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 提供 Azure OpenAI 连接凭据。
/// </summary>
public class AzureOpenAICredential
{
    /// <summary>
    /// 获取 Azure OpenAI 服务端点。
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// 获取 Azure OpenAI API 密钥。
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// 获取模型部署名称。
    /// </summary>
    public string DeploymentName { get; init; } = string.Empty;
}