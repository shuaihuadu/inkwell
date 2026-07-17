// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace Inkwell.VectorStore.InMemory;

/// <summary><see cref="IInkwellBuilder"/> 的进程内默认 VectorStore 唯一入口扩展（dev / unit test）。</summary>
public static class InMemoryVectorStoreBuilderExtensions
{
    /// <summary>注册进程内默认的 <see cref="Microsoft.Extensions.VectorData.VectorStore"/> 实现。</summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseInMemoryVectorStore(this IInkwellBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<Microsoft.Extensions.VectorData.VectorStore>(_ => new InMemoryVectorStore());

        return builder;
    }
}
