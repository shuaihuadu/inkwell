// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// Provider 选择器段；用值型字符串声明每个端口在当前部署下选用哪个 Provider，
/// 供 Builder DSL <c>.UseXxx()</c> 装配期交叉校验（F9）。
/// </summary>
public sealed class InkwellProvidersOptions
{
    /// <summary>
    /// 获取或设置持久化 Provider 名称。
    /// </summary>
    [Required]
    public string Persistence { get; set; } = "PostgreSQL";

    /// <summary>
    /// 获取或设置文件存储 Provider 名称。
    /// </summary>
    [Required]
    public string FileStorage { get; set; } = "LocalFileSystem";

    /// <summary>
    /// 获取或设置缓存 Provider 名称。
    /// </summary>
    [Required]
    public string Cache { get; set; } = "InMemory";

    /// <summary>
    /// 获取或设置队列 Provider 名称。
    /// </summary>
    [Required]
    public string Queue { get; set; } = "Channels";

    /// <summary>
    /// 获取或设置向量存储 Provider 名称。
    /// </summary>
    [Required]
    public string VectorStore { get; set; } = "InMemory";

    /// <summary>
    /// 获取或设置 Agent Runtime Provider 名称。
    /// </summary>
    [Required]
    public string AgentRuntime { get; set; } = "AzureOpenAI";
}
