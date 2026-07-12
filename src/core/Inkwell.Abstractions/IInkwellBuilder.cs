// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// Builder DSL 核心接口，承载 Provider 链式装配；公开属性是 Provider 扩展方法的挂钩点。
/// </summary>
public interface IInkwellBuilder
{
    /// <summary>
    /// 获取服务集合。
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// 获取应用配置。
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// 配置 Inkwell 根选项。
    /// </summary>
    /// <param name="configure">选项配置操作。</param>
    /// <returns>当前 Builder。</returns>
    IInkwellBuilder ConfigureOptions(Action<InkwellOptions> configure);

    /// <summary>
    /// 完成装配并返回服务集合。
    /// </summary>
    /// <returns>已配置的服务集合。</returns>
    IServiceCollection Build();
}
