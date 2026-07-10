using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>注册 <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> 的 Azure OpenAI 实现。</summary>
public static class AzureOpenAIEmbeddingBuilderExtensions
{
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

            if (string.IsNullOrEmpty(options.ApiKey))
            {
                throw new InvalidOperationException("AzureOpenAIEmbeddingOptions.ApiKey is required (v1 supports API key auth only).");
            }

            AzureOpenAIClient client = new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));

            return client.GetEmbeddingClient(options.DeploymentName).AsIEmbeddingGenerator(options.Dimensions);
        });

        return builder;
    }
}
