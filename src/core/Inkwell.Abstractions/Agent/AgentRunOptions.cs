// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// Agent Runtime 端口详细配置。Provider 选择由 <c>InkwellProvidersOptions.AgentRuntime</c> 承载；
/// 具体凭证（如 Azure OpenAI 端点 / 密钥）由 <c>Inkwell.Core</c> 的 <c>AgentRuntime/</c> 目录下独立 Options 承载。
/// </summary>
public sealed class AgentRunOptions
{
    [Range(0.0, 2.0)]
    public double DefaultTemperature { get; set; } = 1.0;

    [Range(0.0, 1.0)]
    public double DefaultTopP { get; set; } = 1.0;

    [Range(1, 128000)]
    public int DefaultMaxTokens { get; set; } = 2048;

    [Range(1, 3600)]
    public int RunTimeoutSeconds { get; set; } = 300;
}
