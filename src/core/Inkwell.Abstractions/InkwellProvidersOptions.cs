// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// Provider 选择器段；用值型字符串声明每个端口在当前部署下选用哪个 Provider，
/// 供 Builder DSL <c>.UseXxx()</c> 装配期交叉校验（F9）。
/// </summary>
public sealed class InkwellProvidersOptions
{
    [Required]
    public string Persistence { get; set; } = "PostgreSQL";

    [Required]
    public string FileStorage { get; set; } = "LocalFileSystem";

    [Required]
    public string Cache { get; set; } = "InMemory";

    [Required]
    public string Queue { get; set; } = "Channels";

    [Required]
    public string VectorStore { get; set; } = "InMemory";

    [Required]
    public string AgentRuntime { get; set; } = "AzureOpenAI";
}
