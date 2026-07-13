// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inkwell;

/// <summary>
/// 提供 Azure OpenAI Agent Factory 的 Builder DSL 注册入口。
/// </summary>
public static class AzureOpenAIAgentFactoryBuilderExtensions
{
    /// <summary>
    /// 注册基于 Azure OpenAI Chat Completions 的 Agent Factory。
    /// </summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <param name="credential">Azure OpenAI 连接凭据。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseAzureOpenAIAgentFactory(this IInkwellBuilder builder, AzureOpenAICredential credential)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(credential);

        builder.Services.TryAddSingleton(credential);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IModelRuntimeAgentBuilder, AzureOpenAIAgentFactory>());
        builder.Services.TryAddSingleton<IAgentFactory, ModelRoutingAgentFactory>();

        return builder;
    }
}