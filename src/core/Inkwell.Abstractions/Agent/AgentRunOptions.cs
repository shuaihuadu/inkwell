// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// Agent Runtime 端口详细配置。Provider 选择由 <c>InkwellProvidersOptions.AgentRuntime</c> 承载；
/// 具体凭证（如 Azure OpenAI 端点 / 密钥）由 <c>Inkwell.Core</c> 的 <c>AgentRuntime/</c> 目录下独立 Options 承载。
/// </summary>
public sealed class AgentRunOptions
{
    /// <summary>
    /// 获取或设置默认采样温度。
    /// </summary>
    [Range(0.0, 2.0)]
    public double DefaultTemperature { get; set; } = 1.0;

    /// <summary>
    /// 获取或设置默认核采样概率阈值。
    /// </summary>
    [Range(0.0, 1.0)]
    public double DefaultTopP { get; set; } = 1.0;

    /// <summary>
    /// 获取或设置默认最大输出令牌数。
    /// </summary>
    [Range(1, 128000)]
    public int DefaultMaxTokens { get; set; } = 2048;

    /// <summary>
    /// 获取或设置 Agent 运行超时时间（秒）。
    /// </summary>
    [Range(1, 3600)]
    public int RunTimeoutSeconds { get; set; } = 300;
}
