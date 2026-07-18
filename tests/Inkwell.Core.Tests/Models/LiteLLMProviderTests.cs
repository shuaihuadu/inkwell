// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inkwell.Core.Tests.Models;

/// <summary>
/// 验证 LiteLLM Provider 的实时模型发现与分类映射。
/// </summary>
[TestClass]
public sealed class LiteLLMProviderTests
{
    /// <summary>
    /// 验证两个 LLM Provider 端口映射到同一个可安全长期复用的 Provider 实例。
    /// </summary>
    [TestMethod]
    public void UseLiteLLM_RegistersSharedSingletonProvider()
    {
        // Arrange
        ServiceCollection services = new();
        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());
        builder.UseLiteLLM(options =>
        {
            options.Endpoint = new Uri("https://litellm.example/");
            options.ApiKey = "test-key";
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        // Act
        LiteLLMProvider concreteProvider = serviceProvider.GetRequiredService<LiteLLMProvider>();
        ILLMProvider llmProvider = serviceProvider.GetRequiredService<ILLMProvider>();
        IChatLLMProvider chatLLMProvider = serviceProvider.GetRequiredService<IChatLLMProvider>();

        // Assert
        Assert.AreSame(concreteProvider, llmProvider);
        Assert.AreSame(concreteProvider, chatLLMProvider);
    }

    /// <summary>
    /// 验证 Provider 合并模型组能力并归一化全部首期模型分类。
    /// </summary>
    [TestMethod]
    public async Task ListModelsAsync_WithKnownModes_MapsCategoriesAndCapabilitiesAsync()
    {
        // Arrange
        using StubHttpMessageHandler handler = new(
            """{"object":"list","data":[{"id":"chat-model","owned_by":"openai"},{"id":"embedding-model","owned_by":"openai"},{"id":"image-model","owned_by":"openai"},{"id":"video-model","owned_by":"openai"}]}""",
            """{"data":[{"model_group":"chat-model","mode":"responses","max_input_tokens":1050000,"max_output_tokens":128000,"supports_function_calling":true,"supports_vision":true,"supported_openai_params":["response_format"],"supports_reasoning":true},{"model_group":"embedding-model","mode":"embedding"},{"model_group":"image-model","mode":"image_generation"},{"model_group":"video-model","mode":"video_generation"}]}""");
        using HttpClient httpClient = CreateHttpClient(handler);
        LiteLLMProvider provider = CreateProvider(httpClient);

        // Act
        IReadOnlyList<LLMModel> models = await provider.ListModelsAsync();

        // Assert
        Assert.AreEqual(LLMModelCategory.Chat, models.Single(model => model.Id == "chat-model").Category);
        Assert.AreEqual(LLMModelCategory.Embedding, models.Single(model => model.Id == "embedding-model").Category);
        Assert.AreEqual(LLMModelCategory.ImageGeneration, models.Single(model => model.Id == "image-model").Category);
        Assert.AreEqual(LLMModelCategory.VideoGeneration, models.Single(model => model.Id == "video-model").Category);
        LLMModel chatModel = models.Single(model => model.Id == "chat-model");
        Assert.AreEqual("responses", chatModel.ProviderMode);
        Assert.AreEqual(1_050_000, chatModel.MaxInputTokens);
        Assert.AreEqual(128_000, chatModel.MaxOutputTokens);
        Assert.IsTrue(chatModel.SupportsVision);
        Assert.IsTrue(chatModel.SupportsTools);
        Assert.IsTrue(chatModel.SupportsStructuredOutput);
        Assert.IsTrue(chatModel.SupportsReasoning);
    }

    /// <summary>
    /// 验证未知 mode 保留原值并归类为 Unknown，缺失能力保持未知。
    /// </summary>
    [TestMethod]
    public async Task ListModelsAsync_WithUnknownMode_PreservesProviderModeAndUnknownCapabilitiesAsync()
    {
        // Arrange
        using StubHttpMessageHandler handler = new(
            """{"data":[{"id":"rerank-model","owned_by":"custom"}]}""",
            """{"data":[{"model_group":"rerank-model","mode":"rerank"}]}""");
        using HttpClient httpClient = CreateHttpClient(handler);
        LiteLLMProvider provider = CreateProvider(httpClient);

        // Act
        LLMModel model = (await provider.ListModelsAsync()).Single();

        // Assert
        Assert.AreEqual(LLMModelCategory.Unknown, model.Category);
        Assert.AreEqual("rerank", model.ProviderMode);
        Assert.AreEqual("custom", model.OwnedBy);
        Assert.IsNull(model.SupportsVision);
        Assert.IsNull(model.SupportsTools);
        Assert.IsNull(model.SupportsStructuredOutput);
        Assert.IsNull(model.SupportsReasoning);
    }

    /// <summary>
    /// 验证 LiteLLM 以带小数点的 JSON number 返回 token 上限时仍能映射为整数。
    /// </summary>
    [TestMethod]
    public async Task ListModelsAsync_WithDecimalTokenLimits_MapsIntegralValuesAsync()
    {
        // Arrange
        using StubHttpMessageHandler handler = new(
            """{"data":[{"id":"decimal-limit-model","owned_by":"custom"}]}""",
            """{"data":[{"model_group":"decimal-limit-model","mode":"chat","max_input_tokens":1050000.0,"max_output_tokens":128000.0}]}""");
        using HttpClient httpClient = CreateHttpClient(handler);
        using LiteLLMProvider provider = CreateProvider(httpClient);

        // Act
        LLMModel model = (await provider.ListModelsAsync()).Single();

        // Assert
        Assert.AreEqual(1_050_000, model.MaxInputTokens);
        Assert.AreEqual(128_000, model.MaxOutputTokens);
    }

    /// <summary>
    /// 验证按 ID 查询使用大小写不敏感匹配。
    /// </summary>
    [TestMethod]
    public async Task GetModelAsync_WithDifferentCasing_ReturnsModelAsync()
    {
        // Arrange
        using StubHttpMessageHandler handler = new(
            """{"data":[{"id":"GPT-5.4","owned_by":"openai"}]}""",
            """{"data":[{"model_group":"gpt-5.4","mode":"chat"}]}""");
        using HttpClient httpClient = CreateHttpClient(handler);
        LiteLLMProvider provider = CreateProvider(httpClient);

        // Act
        LLMModel model = await provider.GetModelAsync("gpt-5.4");

        // Assert
        Assert.AreEqual("GPT-5.4", model.Id);
        Assert.AreEqual(LLMModelCategory.Chat, model.Category);
    }

    /// <summary>
    /// 验证 Chat 模型连通性测试发送一次最小生成请求。
    /// </summary>
    [TestMethod]
    public async Task TestModelAsync_WithChatModel_ReturnsSuccessAsync()
    {
        // Arrange
        using StubHttpMessageHandler handler = new(
            """{"data":[{"id":"gpt-5.4","owned_by":"openai"}]}""",
            """{"data":[{"model_group":"gpt-5.4","mode":"chat"}]}""");
        using HttpClient httpClient = CreateHttpClient(handler);
        LiteLLMProvider provider = CreateProvider(httpClient);

        // Act
        LLMModelTestResult result = await provider.TestModelAsync("gpt-5.4");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("gpt-5.4", result.ModelId);
        Assert.AreEqual(1, handler.ChatRequestCount);
    }

    /// <summary>
    /// 验证非 Chat 模型不会触发可能计费的生成请求。
    /// </summary>
    [TestMethod]
    public async Task TestModelAsync_WithEmbeddingModel_DoesNotSendGenerationRequestAsync()
    {
        // Arrange
        using StubHttpMessageHandler handler = new(
            """{"data":[{"id":"embedding-model","owned_by":"openai"}]}""",
            """{"data":[{"model_group":"embedding-model","mode":"embedding"}]}""");
        using HttpClient httpClient = CreateHttpClient(handler);
        LiteLLMProvider provider = CreateProvider(httpClient);

        // Act
        LLMModelTestResult result = await provider.TestModelAsync("embedding-model");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(0, handler.ChatRequestCount);
    }

    /// <summary>
    /// 验证创建的 Chat Client 通过 OpenAI-compatible 端点发送模型请求并解析响应。
    /// </summary>
    [TestMethod]
    public async Task CreateChatClient_WithModelId_SendsAndParsesChatCompletionAsync()
    {
        // Arrange
        using StubHttpMessageHandler handler = new("""{"data":[]}""", """{"data":[]}""");
        using HttpClient httpClient = CreateHttpClient(handler);
        LiteLLMProvider provider = CreateProvider(httpClient);
        IChatClient chatClient = provider.CreateChatClient("gpt-5.4");

        // Act
        ChatResponse response = await chatClient.GetResponseAsync("Hello");

        // Assert
        Assert.AreEqual("OK", response.Text);
        Assert.AreEqual(1, handler.ChatRequestCount);
        Assert.AreEqual("Bearer", handler.AuthorizationScheme);
        Assert.AreEqual("test-key", handler.AuthorizationParameter);
        using JsonDocument request = JsonDocument.Parse(handler.ChatRequestJson!);
        Assert.AreEqual("gpt-5.4", request.RootElement.GetProperty("model").GetString());
    }

    private static LiteLLMProvider CreateProvider(HttpClient httpClient) => new(
        httpClient,
        Options.Create(new LiteLLMOptions
        {
            Endpoint = httpClient.BaseAddress!,
            ApiKey = "test-key",
        }));

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) => new(handler, disposeHandler: false)
    {
        BaseAddress = new Uri("https://litellm.example/"),
    };

    private sealed class StubHttpMessageHandler(string modelsResponseJson, string groupsResponseJson) : HttpMessageHandler
    {
        public int ChatRequestCount { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        public string? AuthorizationScheme { get; private set; }

        public string? ChatRequestJson { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string responseJson = request.RequestUri?.AbsolutePath switch
            {
                "/v1/models" => modelsResponseJson,
                "/model_group/info" => groupsResponseJson,
                "/v1/chat/completions" => await this.GetChatResponseAsync(request, cancellationToken),
                _ => throw new AssertFailedException($"Unexpected request URI: {request.RequestUri}"),
            };
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };

            return response;
        }

        private async Task<string> GetChatResponseAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.ChatRequestCount++;
            this.AuthorizationScheme = request.Headers.Authorization?.Scheme;
            this.AuthorizationParameter = request.Headers.Authorization?.Parameter;
            this.ChatRequestJson = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return """{"id":"chatcmpl-test","object":"chat.completion","created":1784300000,"model":"gpt-5.4","choices":[{"index":0,"message":{"role":"assistant","content":"OK"},"finish_reason":"stop"}],"usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}""";
        }
    }
}
