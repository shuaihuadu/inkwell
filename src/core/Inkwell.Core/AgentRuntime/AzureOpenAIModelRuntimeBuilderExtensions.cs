// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 提供 Azure OpenAI 模型运行时的 Builder DSL 注册入口。
/// </summary>
public static class AzureOpenAIModelRuntimeBuilderExtensions
{
    /// <summary>
    /// 注册基于 Azure OpenAI Chat Completions 的模型运行时。
    /// </summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <param name="credential">Azure OpenAI 连接凭据。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseAzureOpenAIModelRuntime(this IInkwellBuilder builder, AzureOpenAICredential credential)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(credential);

        builder.Services.TryAddSingleton(credential);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IModelRuntimeChatClientProvider, AzureOpenAIModelRuntimeChatClientProvider>());
        builder.Services.TryAddScoped<IAgentBuildOptionsResolver, AgentBuildOptionsResolver>();
        builder.Services.TryAddScoped<IAgentFactory, ModelRoutingAgentFactory>();
        builder.Services.TryAddScoped<IAgentBuildService, AgentBuildService>();

        return builder;
    }
}