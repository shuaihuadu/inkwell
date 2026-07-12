// Copyright (c) ShuaiHua Du. All rights reserved.

using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>注册 <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> 的 Azure OpenAI 实现。</summary>
public static class AzureOpenAIEmbeddingBuilderExtensions
{
    /// <summary>
    /// 注册 Azure OpenAI 嵌入向量生成器。
    /// </summary>
    /// <param name="builder">Inkwell 构建器。</param>
    /// <param name="configure">Azure OpenAI 嵌入向量配置委托。</param>
    /// <returns>当前 Inkwell 构建器。</returns>
    public static IInkwellBuilder UseAzureOpenAIEmbeddings(this IInkwellBuilder builder, Action<AzureOpenAIEmbeddingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        OptionsBuilder<AzureOpenAIEmbeddingOptions> optionsBuilder = builder.Services.AddOptions<AzureOpenAIEmbeddingOptions>().Bind(builder.Configuration.GetSection("Inkwell:VectorStore:AzureOpenAI"));

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            AzureOpenAIEmbeddingOptions options = sp.GetRequiredService<IOptions<AzureOpenAIEmbeddingOptions>>().Value;

            if (string.IsNullOrEmpty(options.Credential.ApiKey))
            {
                throw new InvalidOperationException("AzureOpenAIEmbeddingOptions.ApiKey is required (v1 supports API key auth only).");
            }

            AzureOpenAIClient client = new(new Uri(options.Credential.Endpoint), new AzureKeyCredential(options.Credential.ApiKey));

            return client.GetEmbeddingClient(options.Credential.DeploymentName).AsIEmbeddingGenerator(options.Dimensions);
        });

        return builder;
    }
}
