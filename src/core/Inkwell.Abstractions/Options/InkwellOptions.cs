using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 根 Options，承载全局设置 + Provider 选择器段 + 各端口子 Options 的引用槽位。
/// </summary>
public sealed class InkwellOptions
{
    [Required]
    public string ServiceName { get; set; } = "inkwell";

    [Required]
    public string Environment { get; set; } = "dev";

    public InkwellProvidersOptions Providers { get; set; } = new();

    public PersistenceOptions Persistence { get; set; } = new();

    public FileStorageOptions FileStorage { get; set; } = new();

    public CacheOptions Cache { get; set; } = new();

    public QueueOptions Queue { get; set; } = new();

    public AgentRuntimeOptions AgentRuntime { get; set; } = new();

    public VectorStoreOptions VectorStore { get; set; } = new();
}
