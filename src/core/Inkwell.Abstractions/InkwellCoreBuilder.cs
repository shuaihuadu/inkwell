using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// Inkwell 核心服务构建器
/// 支持链式配置持久化提供程序和 LLM 客户端
/// </summary>
public sealed class InkwellCoreBuilder(IServiceCollection services)
{
    /// <summary>
    /// 获取服务集合
    /// </summary>
    public IServiceCollection Services { get; } = services;
}
