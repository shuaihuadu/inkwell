// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.AgentRuntime;

/// <summary>
/// 验证模型 Registry 到 MAF 运行时连接器的路由行为。
/// </summary>
[TestClass]
public sealed class ModelRoutingAgentFactoryTests
{
    /// <summary>
    /// 验证可用模型会按 RuntimeId 路由到对应连接器。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithAvailableModel_RoutesByRuntimeIdAsync()
    {
        // Arrange
        ModelDefinition model = CreateModel("qwen", "litellm", isAvailable: true);
        StubModelRuntimeAgentBuilder azureRuntime = new("azure-openai");
        StubModelRuntimeAgentBuilder liteLLMRuntime = new("litellm");
        ModelRoutingAgentFactory factory = new(new StubModelRegistryService(model), [azureRuntime, liteLLMRuntime]);
        AgentVersion version = CreateVersion(model.Id);

        // Act
        AIAgent agent = await factory.BuildAsync(version, new AgentBuildOptions());

        // Assert
        Assert.IsNotNull(agent);
        Assert.IsFalse(azureRuntime.WasCalled);
        Assert.IsTrue(liteLLMRuntime.WasCalled);
        Assert.AreSame(model, liteLLMRuntime.Model);
    }

    /// <summary>
    /// 验证已发现但元数据未补齐的模型不会进入 MAF 调用层。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithUnavailableModel_ThrowsBeforeRuntimeAsync()
    {
        // Arrange
        ModelDefinition model = CreateModel("qwen", "litellm", isAvailable: false);
        StubModelRuntimeAgentBuilder runtime = new("litellm");
        ModelRoutingAgentFactory factory = new(new StubModelRegistryService(model), [runtime]);

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.BuildAsync(CreateVersion(model.Id), new AgentBuildOptions()).AsTask());

        // Assert
        StringAssert.Contains(exception.Message, "metadata is incomplete");
        Assert.IsFalse(runtime.WasCalled);
    }

    /// <summary>
    /// 验证模型声明了未注册的 RuntimeId 时返回明确错误。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithUnknownRuntime_ThrowsAsync()
    {
        // Arrange
        ModelDefinition model = CreateModel("claude", "anthropic-native", isAvailable: true);
        ModelRoutingAgentFactory factory = new(new StubModelRegistryService(model), []);

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.BuildAsync(CreateVersion(model.Id), new AgentBuildOptions()).AsTask());

        // Assert
        StringAssert.Contains(exception.Message, "anthropic-native");
    }

    private static ModelDefinition CreateModel(string id, string runtimeId, bool isAvailable) => new()
    {
        Id = id,
        DisplayName = id,
        SourceId = runtimeId == "litellm" ? "litellm" : "configuration",
        RuntimeId = runtimeId,
        RemoteModelId = id,
        IsAvailable = isAvailable,
        UnavailableReason = isAvailable ? null : "Model metadata is incomplete.",
    };

    private static AgentVersion CreateVersion(string modelId) => new()
    {
        Id = Guid.CreateVersion7(),
        AgentId = Guid.CreateVersion7(),
        VersionNumber = 1,
        Status = AgentVersionStatus.Draft,
        CreatedByUserId = Guid.CreateVersion7(),
        Snapshot = new AgentSnapshot
        {
            Name = "Test agent",
            Instructions = "Test instructions",
            ModelId = modelId,
        },
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private sealed class StubModelRegistryService(ModelDefinition model) : IModelRegistryService
    {
        public Task<IReadOnlyList<ModelDefinition>> ListModelsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ModelDefinition>>([model]);

        public Task<ModelDefinition> GetModelAsync(string modelId, CancellationToken cancellationToken = default) =>
            Task.FromResult(model);
    }

    private sealed class StubModelRuntimeAgentBuilder(string runtimeId) : IModelRuntimeAgentBuilder
    {
        public string RuntimeId => runtimeId;

        public bool WasCalled { get; private set; }

        public ModelDefinition? Model { get; private set; }

        public AIAgent Build(
            ModelDefinition model,
            AgentVersion agentVersion,
            AgentBuildOptions agentBuildOptions,
            CancellationToken cancellationToken = default)
        {
            this.WasCalled = true;
            this.Model = model;
            AzureOpenAIAgentFactory factory = new(new AzureOpenAICredential
            {
                Endpoint = "https://example.openai.azure.com",
                ApiKey = "test-api-key",
            });
            return factory.Build(model, agentVersion, agentBuildOptions, cancellationToken);
        }
    }
}
