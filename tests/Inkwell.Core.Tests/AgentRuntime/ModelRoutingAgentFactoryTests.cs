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
        StubModelRuntimeChatClientProvider azureRuntime = new("azure-openai");
        StubModelRuntimeChatClientProvider liteLLMRuntime = new("litellm");
        ModelRoutingAgentFactory factory = CreateFactory(model, [azureRuntime, liteLLMRuntime]);
        AgentVersion version = CreateVersion(model.Id);

        // Act
        AIAgent agent = await factory.BuildAsync(version);

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
        StubModelRuntimeChatClientProvider runtime = new("litellm");
        ModelRoutingAgentFactory factory = CreateFactory(model, [runtime]);

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.BuildAsync(CreateVersion(model.Id)).AsTask());

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
        ModelRoutingAgentFactory factory = CreateFactory(model, []);

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.BuildAsync(CreateVersion(model.Id)).AsTask());

        // Assert
        StringAssert.Contains(exception.Message, "anthropic-native");
    }

    /// <summary>
    /// 验证 Inkwell Skill 定义只在模型运行时边界转换为 MAF 对象。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithSkillDefinitions_ResolvesRuntimeObjectsAsync()
    {
        // Arrange
        ModelDefinition model = CreateModel("qwen", "litellm", isAvailable: true);
        StubModelRuntimeChatClientProvider runtime = new("litellm");
        ModelRoutingAgentFactory factory = CreateFactory(model, [runtime]);
        AgentBuildOptions buildOptions = new()
        {
            ModelOptions = new AgentModelOptions { ModelId = model.Id },
            Skills = [CreateSkillDefinition()],
        };
        AgentVersion version = CreateVersion(model.Id, buildOptions);

        // Act
        AIAgent agent = await factory.BuildAsync(version);

        // Assert
        ChatClientAgent chatClientAgent = (ChatClientAgent)agent;
        List<AIContextProvider> contextProviders = chatClientAgent.AIContextProviders?.ToList() ?? [];
        Assert.HasCount(1, contextProviders);
        Assert.IsInstanceOfType<AgentSkillsProvider>(contextProviders[0]);
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

    private static ModelRoutingAgentFactory CreateFactory(
        ModelDefinition model,
        IEnumerable<IModelRuntimeChatClientProvider> runtimeClientProviders) =>
        new(new StubModelRegistryService(model), runtimeClientProviders, new StubPersistenceProvider());

    private static AgentSkillDefinition CreateSkillDefinition() => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "source-review",
        Description = "Reviews sources.",
        Content = "Review every source before citing it.",
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private static AgentVersion CreateVersion(string modelId, AgentBuildOptions? buildOptions = null)
    {
        Guid agentId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return new AgentVersion
        {
            Id = versionId,
            AgentId = agentId,
            VersionNumber = 1,
            Snapshot = new AgentSnapshot
            {
                Name = "Test agent",
                Instructions = "Test instructions",
                BuildOptions = buildOptions ?? new AgentBuildOptions
                {
                    ModelOptions = new AgentModelOptions { ModelId = modelId },
                },
            },
            CreatedByUserId = Guid.CreateVersion7(),
            CreatedTime = now,
            UpdatedTime = now,
            PublishedTime = now,
        };
    }

    private sealed class StubModelRegistryService(ModelDefinition model) : IModelRegistryService
    {
        public Task<IReadOnlyList<ModelDefinition>> ListModelsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ModelDefinition>>([model]);

        public Task<ModelDefinition> GetModelAsync(string modelId, CancellationToken cancellationToken = default) =>
            Task.FromResult(model);
    }

    private sealed class StubModelRuntimeChatClientProvider(string runtimeId) : IModelRuntimeChatClientProvider
    {
        public string RuntimeId => runtimeId;

        public bool WasCalled { get; private set; }

        public ModelDefinition? Model { get; private set; }

        public IChatClient GetChatClient(ModelDefinition model)
        {
            this.WasCalled = true;
            this.Model = model;
            AzureOpenAIModelRuntimeChatClientProvider provider = new(new AzureOpenAICredential
            {
                Endpoint = "https://example.openai.azure.com",
                ApiKey = "test-api-key",
            });
            return provider.GetChatClient(model);
        }
    }

    private sealed class StubPersistenceProvider : IPersistenceProvider
    {
        public TRepository GetRepository<TRepository>() where TRepository : notnull =>
            throw new NotSupportedException();

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
