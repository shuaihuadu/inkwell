// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.Options;

namespace Inkwell.Core.Tests.Models;

/// <summary>
/// 验证模型注册表的来源聚合与配置映射行为。
/// </summary>
[TestClass]
public sealed class ModelRegistryServiceTests
{
    /// <summary>
    /// 验证注册表会聚合配置与 LiteLLM 来源并按显示名称排序。
    /// </summary>
    [TestMethod]
    public async Task ListModelsAsync_WithMultipleSources_AggregatesModelsAsync()
    {
        // Arrange
        ModelRegistryService service = new([
            new StubModelRegistrySource("configuration", CreateModel("native", "Zulu", "configuration")),
            new StubModelRegistrySource("litellm", CreateModel("gateway", "Alpha", "litellm")),
        ]);

        // Act
        IReadOnlyList<ModelDefinition> models = await service.ListModelsAsync();

        // Assert
        Assert.HasCount(2, models);
        Assert.AreEqual("gateway", models[0].Id);
        Assert.AreEqual("native", models[1].Id);
    }

    /// <summary>
    /// 验证不同来源出现相同模型标识时会拒绝返回歧义结果。
    /// </summary>
    [TestMethod]
    public async Task ListModelsAsync_WithDuplicateIds_ThrowsAsync()
    {
        // Arrange
        ModelRegistryService service = new([
            new StubModelRegistrySource("configuration", CreateModel("shared", "Native", "configuration")),
            new StubModelRegistrySource("litellm", CreateModel("SHARED", "Gateway", "litellm")),
        ]);

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ListModelsAsync());

        // Assert
        StringAssert.Contains(exception.Message, "Duplicate model ID 'shared'");
        StringAssert.Contains(exception.Message, "configuration");
        StringAssert.Contains(exception.Message, "litellm");
    }

    /// <summary>
    /// 验证未知模型错误使用当前 Registry 术语。
    /// </summary>
    [TestMethod]
    public async Task GetModelAsync_WithUnknownId_UsesRegistryTerminologyAsync()
    {
        // Arrange
        ModelRegistryService service = new([]);

        // Act
        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetModelAsync("missing"));

        // Assert
        StringAssert.Contains(exception.Message, "registered in the registry");
    }

    /// <summary>
    /// 验证配置来源会保留发布方、家族、远端模型标识与能力元数据。
    /// </summary>
    [TestMethod]
    public async Task ConfigurationSource_WithNativeModel_MapsBusinessMetadataAsync()
    {
        // Arrange
        ConfigurationModelRegistryOptions registryOptions = new()
        {
            Models = [new ConfigurationModelEntryOptions
            {
                Id = "qwen-native",
                DisplayName = "Qwen Native",
                PublisherId = "alibaba",
                PublisherDisplayName = "Alibaba Cloud",
                FamilyId = "qwen",
                FamilyDisplayName = "Qwen",
                RuntimeId = "dashscope-native",
                RemoteModelId = "qwen-max",
                SupportsTools = true,
                ContextWindowTokens = 32768,
            }],
        };
        ConfigurationModelRegistrySource source = new(Options.Create(registryOptions));

        // Act
        ModelDefinition model = (await source.ListModelsAsync()).Single();

        // Assert
        Assert.AreEqual("configuration", model.SourceId);
        Assert.AreEqual("alibaba", model.PublisherId);
        Assert.AreEqual("qwen", model.FamilyId);
        Assert.AreEqual("dashscope-native", model.RuntimeId);
        Assert.AreEqual("qwen-max", model.RemoteModelId);
        Assert.IsTrue(model.SupportsTools);
        Assert.AreEqual(32768, model.ContextWindowTokens);
        Assert.IsTrue(model.IsAvailable);
    }

    private static ModelDefinition CreateModel(string id, string displayName, string sourceId) => new()
    {
        Id = id,
        DisplayName = displayName,
        SourceId = sourceId,
        RuntimeId = sourceId,
        RemoteModelId = id,
        IsAvailable = true,
    };

    private sealed class StubModelRegistrySource(string sourceId, params ModelDefinition[] models) : IModelRegistrySource
    {
        public string SourceId => sourceId;

        public Task<IReadOnlyList<ModelDefinition>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ModelDefinition>>(models);
        }
    }
}
