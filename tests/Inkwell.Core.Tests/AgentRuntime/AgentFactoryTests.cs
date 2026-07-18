// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.Options;

namespace Inkwell.Core.Tests.AgentRuntime;

/// <summary>
/// 验证 LLM Provider 到 MAF Agent 的构建行为。
/// </summary>
[TestClass]
public sealed class AgentFactoryTests
{
    /// <summary>
    /// 验证 Chat 模型使用同一模型标识创建聊天客户端。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithChatModel_CreatesChatClientAsync()
    {
        // Arrange
        LLMModel model = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        StubChatLLMProvider chatProvider = new();
        AgentFactory factory = CreateFactory(model, chatProvider);

        // Act
        AIAgent agent = await factory.BuildAsync(CreateVersion(model.Id));

        // Assert
        Assert.IsNotNull(agent);
        Assert.AreEqual(model.Id, chatProvider.ModelId);
        Assert.AreEqual(1, chatProvider.CallCount);
    }

    /// <summary>
    /// 验证非 Chat 分类不会进入聊天客户端创建层。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithNonChatCategory_ThrowsBeforeCreatingClientAsync()
    {
        // Arrange
        LLMModel model = CreateModel("text-embedding-3-large", LLMModelCategory.Embedding);
        StubChatLLMProvider chatProvider = new();
        AgentFactory factory = CreateFactory(model, chatProvider);

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.BuildAsync(CreateVersion(model.Id)).AsTask());

        // Assert
        StringAssert.Contains(exception.Message, "category is 'Embedding'");
        Assert.AreEqual(0, chatProvider.CallCount);
    }

    /// <summary>
    /// 验证 Inkwell Skill 定义只在模型运行时边界转换为 MAF 对象。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithSkillDefinitions_ResolvesRuntimeObjectsAsync()
    {
        // Arrange
        LLMModel model = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        StubChatLLMProvider chatProvider = new();
        AgentFactory factory = CreateFactory(model, chatProvider);
        AgentBuildOptions buildOptions = new()
        {
            ModelOptions = new AgentModelOptions { ModelId = model.Id },
            Skills = [CreateSkillDefinition()],
        };

        // Act
        AIAgent agent = await factory.BuildAsync(CreateVersion(model.Id, buildOptions));

        // Assert
        ChatClientAgent chatClientAgent = (ChatClientAgent)agent;
        List<AIContextProvider> contextProviders = chatClientAgent.AIContextProviders?.ToList() ?? [];
        Assert.HasCount(1, contextProviders);
        Assert.IsInstanceOfType<AgentSkillsProvider>(contextProviders[0]);
    }

    private static LLMModel CreateModel(string id, LLMModelCategory category) => new()
    {
        Id = id,
        Category = category,
        ProviderMode = category == LLMModelCategory.Chat ? "chat" : "embedding",
    };

    private static AgentFactory CreateFactory(LLMModel model, StubChatLLMProvider chatProvider) =>
        new(new StubLLMProvider(model), chatProvider, new StubPersistenceProvider());

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

    private sealed class StubLLMProvider(LLMModel model) : ILLMProvider
    {
        public Task<IReadOnlyList<LLMModel>> ListModelsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LLMModel>>([model]);

        public Task<LLMModel> GetModelAsync(string modelId, CancellationToken cancellationToken = default) =>
            Task.FromResult(model);

        public Task<LLMModelTestResult> TestModelAsync(
            string modelId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubChatLLMProvider : IChatLLMProvider
    {
        private static readonly HttpClient client = new()
        {
            BaseAddress = new Uri("https://litellm.example/"),
        };

        public int CallCount { get; private set; }

        public string? ModelId { get; private set; }

        public IChatClient CreateChatClient(string modelId)
        {
            this.CallCount++;
            this.ModelId = modelId;
            LiteLLMProvider provider = new(
                client,
                Options.Create(new LiteLLMOptions
                {
                    Endpoint = new Uri("https://litellm.example/"),
                    ApiKey = "test-key",
                }));
            return provider.CreateChatClient(modelId);
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
