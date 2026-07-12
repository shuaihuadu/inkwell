// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 根 Options，承载全局设置 + Provider 选择器段 + 各端口子 Options 的引用槽位。
/// </summary>
public sealed class InkwellOptions
{
    /// <summary>
    /// 获取或设置服务名称。
    /// </summary>
    [Required]
    public string ServiceName { get; set; } = "inkwell";

    /// <summary>
    /// 获取或设置运行环境名称。
    /// </summary>
    [Required]
    public string Environment { get; set; } = "dev";

    /// <summary>
    /// 获取或设置 Provider 选择配置。
    /// </summary>
    public InkwellProvidersOptions Providers { get; set; } = new();

    /// <summary>
    /// 获取或设置持久化配置。
    /// </summary>
    public PersistenceOptions Persistence { get; set; } = new();

    /// <summary>
    /// 获取或设置文件存储配置。
    /// </summary>
    public FileStorageOptions FileStorage { get; set; } = new();

    /// <summary>
    /// 获取或设置缓存配置。
    /// </summary>
    public CacheOptions Cache { get; set; } = new();

    /// <summary>
    /// 获取或设置队列配置。
    /// </summary>
    public QueueOptions Queue { get; set; } = new();

    /// <summary>
    /// 获取或设置 Agent 运行配置。
    /// </summary>
    public AgentRunOptions AgentRuntime { get; set; } = new();

    /// <summary>
    /// 获取或设置向量存储配置。
    /// </summary>
    public VectorStoreOptions VectorStore { get; set; } = new();
}
