// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.AppHost;

/// <summary>
/// 表示本地开发环境启动 LiteLLM 时预置的模型部署。
/// </summary>
internal sealed class LiteLLMBootstrapModelOptions
{
    /// <summary>
    /// 获取或设置是否在本次本地启动中预置该模型部署。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 LiteLLM 对外公开的模型名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 LiteLLM 调用的 Provider 模型名称。
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置保存 API Key 的 Aspire Parameter 名称。
    /// </summary>
    public string ApiKeyParameter { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置注入 LiteLLM 容器的 API Key 环境变量名称。
    /// </summary>
    public string ApiKeyEnvironmentVariable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置保存 API Base 的 Aspire Parameter 名称。
    /// </summary>
    public string? ApiBaseParameter { get; set; }

    /// <summary>
    /// 获取或设置注入 LiteLLM 容器的 API Base 环境变量名称。
    /// </summary>
    public string? ApiBaseEnvironmentVariable { get; set; }

    /// <summary>
    /// 获取或设置可选的 Provider API 版本。
    /// </summary>
    public string? ApiVersion { get; set; }
}