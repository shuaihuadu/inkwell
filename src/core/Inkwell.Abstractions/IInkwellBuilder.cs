// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// Builder DSL 核心接口，承载 Provider 链式装配；公开属性是 Provider 扩展方法的挂钩点。
/// </summary>
public interface IInkwellBuilder
{
    IServiceCollection Services { get; }

    IConfiguration Configuration { get; }

    IInkwellBuilder ConfigureOptions(Action<InkwellOptions> configure);

    IServiceCollection Build();
}
