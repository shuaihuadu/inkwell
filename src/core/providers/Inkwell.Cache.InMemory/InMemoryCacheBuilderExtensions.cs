// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inkwell.Cache.InMemory;

/// <summary><see cref="IInkwellBuilder"/> 的进程内默认 Cache Provider 唯一入口扩展（dev / unit test）。</summary>
public static class InMemoryCacheBuilderExtensions
{
    /// <summary>注册进程内默认的 <see cref="ICacheProvider"/> 实现。</summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseInMemoryCache(this IInkwellBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ICacheProvider, InMemoryCacheProvider>();

        return builder;
    }
}
