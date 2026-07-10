// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Inkwell.Queue.Channels;

/// <summary><see cref="IInkwellBuilder"/> 的进程内默认 Queue Provider 唯一入口扩展（dev / unit test）。</summary>
public static class ChannelsQueueBuilderExtensions
{
    /// <summary>注册进程内默认的 <see cref="IQueueProvider"/> 实现。</summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseChannelsQueue(this IInkwellBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IQueueProvider, ChannelsQueueProvider>();

        return builder;
    }
}
