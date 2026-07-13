// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Net;
using System.Text;
using Microsoft.Extensions.Options;

namespace Inkwell.Core.Tests.Models;

/// <summary>
/// 验证 LiteLLM 模型自动发现与业务元数据门禁。
/// </summary>
[TestClass]
public sealed class LiteLLMModelRegistrySourceTests
{
    /// <summary>
    /// 验证 LiteLLM 元数据条目的 DataAnnotations 会被递归校验。
    /// </summary>
    [TestMethod]
    public void OptionsValidator_WithInvalidMetadata_ReturnsFailure()
    {
        // Arrange
        LiteLLMModelRegistryOptions options = CreateOptions([new LiteLLMModelMetadataOptions
        {
            Id = "qwen-plus",
            ContextWindowTokens = 0,
        }]);
        LiteLLMModelRegistryOptionsValidator validator = new();

        // Act
        ValidateOptionsResult result = validator.Validate(null, options);

        // Assert
        Assert.IsTrue(result.Failed);
        StringAssert.Contains(string.Join(" ", result.Failures), "ContextWindowTokens");
    }

    /// <summary>
    /// 验证 LiteLLM 元数据标识按大小写不敏感规则拒绝重复项。
    /// </summary>
    [TestMethod]
    public void OptionsValidator_WithDuplicateMetadataIds_ReturnsFailure()
    {
        // Arrange
        LiteLLMModelRegistryOptions options = CreateOptions([
            new LiteLLMModelMetadataOptions { Id = "qwen-plus" },
            new LiteLLMModelMetadataOptions { Id = "QWEN-PLUS" },
        ]);
        LiteLLMModelRegistryOptionsValidator validator = new();

        // Act
        ValidateOptionsResult result = validator.Validate(null, options);

        // Assert
        Assert.IsTrue(result.Failed);
        StringAssert.Contains(string.Join(" ", result.Failures), "Duplicate LiteLLM model metadata Id(s): qwen-plus.");
    }

    /// <summary>
    /// 验证未配置业务元数据的新模型仍会被发现，但不可用于构建 Agent。
    /// </summary>
    [TestMethod]
    public async Task ListModelsAsync_WithoutMetadata_ReturnsUnavailableDiscoveredModelAsync()
    {
        // Arrange
        using StubHttpMessageHandler handler = new("""
            {"object":"list","data":[{"id":"qwen-plus","object":"model","owned_by":"alibaba"}]}
            """);
        using HttpClient httpClient = CreateHttpClient(handler);
        LiteLLMModelRegistrySource source = CreateSource([], httpClient);

        // Act
        ModelDefinition model = (await source.ListModelsAsync()).Single();

        // Assert
        Assert.AreEqual("qwen-plus", model.Id);
        Assert.AreEqual("litellm", model.SourceId);
        Assert.AreEqual("alibaba", model.PublisherId);
        Assert.AreEqual("qwen-plus", model.RemoteModelId);
        Assert.IsFalse(model.IsAvailable);
        Assert.AreEqual("Model metadata has not been configured in Inkwell.", model.UnavailableReason);
    }

    /// <summary>
    /// 验证元数据完整且启用的 LiteLLM 模型会向业务公开发布方、家族与能力。
    /// </summary>
    [TestMethod]
    public async Task ListModelsAsync_WithCompleteEnabledMetadata_ReturnsAvailableModelAsync()
    {
        // Arrange
        LiteLLMModelMetadataOptions metadata = new()
        {
            Id = "qwen-plus",
            DisplayName = "Qwen Plus",
            PublisherId = "alibaba",
            PublisherDisplayName = "Alibaba Cloud",
            FamilyId = "qwen",
            FamilyDisplayName = "Qwen",
            SupportsTools = true,
            IsEnabled = true,
        };
        using StubHttpMessageHandler handler = new("""
            {"object":"list","data":[{"id":"qwen-plus","object":"model","owned_by":"openai"}]}
            """);
        using HttpClient httpClient = CreateHttpClient(handler);
        LiteLLMModelRegistrySource source = CreateSource([metadata], httpClient);

        // Act
        ModelDefinition model = (await source.ListModelsAsync()).Single();

        // Assert
        Assert.AreEqual("Qwen Plus", model.DisplayName);
        Assert.AreEqual("alibaba", model.PublisherId);
        Assert.AreEqual("qwen", model.FamilyId);
        Assert.IsTrue(model.SupportsTools);
        Assert.IsTrue(model.IsAvailable);
        Assert.IsNull(model.UnavailableReason);
    }

    private static LiteLLMModelRegistrySource CreateSource(
        IReadOnlyList<LiteLLMModelMetadataOptions> metadata,
        HttpClient httpClient)
    {
        LiteLLMModelRegistryOptions registryOptions = CreateOptions(metadata, httpClient.BaseAddress!);

        return new LiteLLMModelRegistrySource(httpClient, Options.Create(registryOptions));
    }

    private static LiteLLMModelRegistryOptions CreateOptions(
        IReadOnlyList<LiteLLMModelMetadataOptions> metadata,
        Uri? endpoint = null) => new()
    {
        Endpoint = endpoint ?? new Uri("https://litellm.example/"),
        ApiKey = "test-key",
        Models = metadata,
    };

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) => new(handler, disposeHandler: false)
    {
        BaseAddress = new Uri("https://litellm.example/"),
    };

    private sealed class StubHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.AreEqual("https://litellm.example/v1/models", request.RequestUri?.AbsoluteUri);

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };

            return Task.FromResult(response);
        }
    }
}
