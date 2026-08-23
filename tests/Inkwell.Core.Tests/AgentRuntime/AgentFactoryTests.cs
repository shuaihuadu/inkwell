// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Diagnostics;
using System.Diagnostics.Metrics;
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
        using StubChatLLMProvider chatProvider = new();
        AgentFactory factory = CreateFactory(model, chatProvider);

        // Act
        AIAgent agent = await factory.BuildAsync(CreateVersion(model.Id));

        // Assert
        Assert.IsNotNull(agent);
        Assert.IsInstanceOfType<OpenTelemetryAgent>(agent);
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
        using StubChatLLMProvider chatProvider = new();
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
        using StubChatLLMProvider chatProvider = new();
        AgentFactory factory = CreateFactory(model, chatProvider);
        AgentBuildOptions buildOptions = new()
        {
            ModelOptions = new AgentModelOptions { ModelId = model.Id },
            Skills = [CreateSkillDefinition()],
        };

        // Act
        AIAgent agent = await factory.BuildAsync(CreateVersion(model.Id, buildOptions));

        // Assert
        ChatClientAgent? chatClientAgent = agent.GetService<ChatClientAgent>();
        Assert.IsNotNull(chatClientAgent);
        List<AIContextProvider> contextProviders = chatClientAgent.AIContextProviders?.ToList() ?? [];
        Assert.HasCount(1, contextProviders);
        AgentSkillsProvider skillsProvider = Assert.IsInstanceOfType<AgentSkillsProvider>(contextProviders[0]);
        AIContextProvider.InvokingContext invokingContext = new(chatClientAgent, session: null, new AIContext());
        AIContext context = await skillsProvider.InvokingAsync(invokingContext, CancellationToken.None);
        AITool? loadSkill = context.Tools?.Single(tool => tool.Name == AgentSkillsProvider.LoadSkillToolName);
        AITool? readSkillResource = context.Tools?.Single(tool => tool.Name == AgentSkillsProvider.ReadSkillResourceToolName);
        AITool? runSkillScript = context.Tools?.Single(tool => tool.Name == AgentSkillsProvider.RunSkillScriptToolName);
        Assert.IsNotNull(loadSkill);
        Assert.IsNotNull(readSkillResource);
        Assert.IsNotNull(runSkillScript);
        Assert.IsNotInstanceOfType<ApprovalRequiredAIFunction>(loadSkill);
        Assert.IsNotInstanceOfType<ApprovalRequiredAIFunction>(readSkillResource);
        Assert.IsNotInstanceOfType<ApprovalRequiredAIFunction>(runSkillScript);
    }

    /// <summary>
    /// 验证已绑定的内置工具在运行时转换为 MAF AIFunction。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithCurrentDateTimeTool_AddsAIFunctionAsync()
    {
        // Arrange
        LLMModel model = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        AgentToolDefinition tool = CreateToolDefinition("get_current_datetime");
        CapturingChatLLMProvider chatProvider = new();
        AgentFactory factory = CreateFactory(model, chatProvider, tool);
        AgentBuildOptions buildOptions = new()
        {
            ModelOptions = new AgentModelOptions { ModelId = model.Id },
            ToolBindings = [new AgentToolBinding(tool.Id, null)],
        };

        // Act
        AIAgent agent = await factory.BuildAsync(CreateVersion(model.Id, buildOptions));
        await agent.RunAsync([new ChatMessage(ChatRole.User, "What time is it?")]);

        // Assert
        AIFunction function = Assert.IsInstanceOfType<AIFunction>(chatProvider.LastOptions?.Tools?.Single());
        Assert.AreEqual(tool.Name, function.Name);
        Assert.AreEqual(tool.Description, function.Description);
        object? result = await function.InvokeAsync(new AIFunctionArguments
        {
            ["timeZoneId"] = "Asia/Shanghai",
        });
        JsonElement resultElement = Assert.IsInstanceOfType<JsonElement>(result);
        Assert.AreEqual("Asia/Shanghai", resultElement.GetProperty("timeZoneId").GetString());
    }

    /// <summary>
    /// 验证未绑定工具的 Agent 不获得内置工具。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithoutToolBindings_DoesNotAddToolsAsync()
    {
        // Arrange
        LLMModel model = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        CapturingChatLLMProvider chatProvider = new();
        AgentFactory factory = CreateFactory(model, chatProvider);

        // Act
        AIAgent agent = await factory.BuildAsync(CreateVersion(model.Id));
        await agent.RunAsync([new ChatMessage(ChatRole.User, "Hello")]);

        // Assert
        Assert.IsEmpty(chatProvider.LastOptions?.Tools ?? []);
    }

    /// <summary>
    /// 验证目录中存在但运行时未实现的工具会使 Agent 构建失败。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithUnavailableTool_ThrowsInvalidOperationExceptionAsync()
    {
        // Arrange
        LLMModel model = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        AgentToolDefinition tool = CreateToolDefinition("unavailable_tool");
        AgentFactory factory = CreateFactory(model, new RespondingChatLLMProvider(), tool);
        AgentBuildOptions buildOptions = new()
        {
            ModelOptions = new AgentModelOptions { ModelId = model.Id },
            ToolBindings = [new AgentToolBinding(tool.Id, null)],
        };

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.BuildAsync(CreateVersion(model.Id, buildOptions)).AsTask());

        // Assert
        StringAssert.Contains(exception.Message, "is not available in the Agent runtime");
    }

    /// <summary>
    /// 验证默认内存历史能够恢复被 jsonb 重排过多态元数据的 Session 状态。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task BuildAsync_WithoutPersistentHistory_RestoresJsonbReorderedSessionAsync()
    {
        // Arrange
        LLMModel model = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        using StubChatLLMProvider chatProvider = new();
        AgentFactory factory = CreateFactory(model, chatProvider);
        AIAgent agent = await factory.BuildAsync(CreateVersion(model.Id));
        ChatClientAgent? chatClientAgent = agent.GetService<ChatClientAgent>();
        Assert.IsNotNull(chatClientAgent);
        InMemoryChatHistoryProvider provider = Assert.IsInstanceOfType<InMemoryChatHistoryProvider>(chatClientAgent.ChatHistoryProvider);
        AgentSession session = await agent.CreateSessionAsync();
        provider.SetMessages(session, [new ChatMessage(ChatRole.User, "hello")]);
        JsonElement serializedState = await agent.SerializeSessionAsync(session, AgentSessionJsonOptions.Default);
        JsonElement reorderedState = MoveMetadataPropertiesToEnd(serializedState);

        // Act
        AgentSession restoredSession = await agent.DeserializeSessionAsync(reorderedState, AgentSessionJsonOptions.Default);
        List<ChatMessage> restoredMessages = provider.GetMessages(restoredSession);

        // Assert
        Assert.HasCount(1, restoredMessages);
        Assert.AreEqual(ChatRole.User, restoredMessages[0].Role);
        Assert.AreEqual("hello", restoredMessages[0].Text);
    }

    /// <summary>
    /// 验证产品会话构建始终使用 Inkwell 外部持久化聊天历史。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task BuildConversationAsync_WithoutConfiguredHistory_UsesInkwellProviderAsync()
    {
        // Arrange
        LLMModel model = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        using StubChatLLMProvider chatProvider = new();
        AgentFactory factory = CreateFactory(model, chatProvider);

        // Act
        AIAgent agent = await factory.BuildConversationAsync(CreateVersion(model.Id));

        // Assert
        ChatClientAgent? chatClientAgent = agent.GetService<ChatClientAgent>();
        Assert.IsNotNull(chatClientAgent);
        Assert.IsInstanceOfType<InkwellChatHistoryProvider>(chatClientAgent.ChatHistoryProvider);
    }

    /// <summary>
    /// 验证运行 Agent 时发出 GenAI trace 与标准指标，且不采集消息正文。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task BuildAsync_WhenRun_EmitsGenAITelemetryWithoutSensitiveDataAsync()
    {
        // Arrange
        List<Activity> activities = [];
        List<string> instruments = [];
        using ActivityListener activityListener = new()
        {
            ShouldListenTo = source => source.Name == AgentFactory.TelemetrySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add,
        };
        ActivitySource.AddActivityListener(activityListener);

        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AgentFactory.TelemetrySourceName)
            {
                instruments.Add(instrument.Name);
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((_, _, _, _) => { });
        meterListener.SetMeasurementEventCallback<double>((_, _, _, _) => { });
        meterListener.Start();

        LLMModel model = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        AgentFactory factory = CreateFactory(model, new RespondingChatLLMProvider());
        AIAgent agent = await factory.BuildAsync(CreateVersion(model.Id));

        // Act
        await agent.RunAsync([new ChatMessage(ChatRole.User, "hello")]);

        // Assert
        OpenTelemetryAgent telemetryAgent = Assert.IsInstanceOfType<OpenTelemetryAgent>(agent);
        Assert.IsFalse(telemetryAgent.EnableSensitiveData);
        Assert.IsTrue(activities.Any(activity => Equals(activity.GetTagItem("gen_ai.operation.name"), "invoke_agent")));
        CollectionAssert.Contains(instruments, "gen_ai.client.operation.duration");
        CollectionAssert.Contains(instruments, "gen_ai.client.token.usage");
    }

    private static LLMModel CreateModel(string id, LLMModelCategory category) => new()
    {
        Id = id,
        Category = category,
        ProviderMode = category == LLMModelCategory.Chat ? "chat" : "embedding",
    };

    private static AgentFactory CreateFactory(
        LLMModel model,
        IChatLLMProvider chatProvider,
        AgentToolDefinition? tool = null) =>
        new(
            new StubLLMProvider(model),
            chatProvider,
            new StubAgentToolCatalogService(tool),
            new StubPersistenceProvider(),
            TimeProvider.System);

    private static AgentToolDefinition CreateToolDefinition(string name) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        Description = "Gets the current date and time.",
        ParametersJsonSchema = "{\"type\":\"object\",\"required\":[]}",
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private static AgentSkillDefinition CreateSkillDefinition() => new()
    {
        Id = Guid.CreateVersion7(),
        OwnerUserId = Guid.CreateVersion7(),
        Name = "source-review",
        Description = "Reviews sources.",
        Content = "Review every source before citing it.",
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private static JsonElement MoveMetadataPropertiesToEnd(JsonElement value)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            WriteWithMetadataLast(writer, value);
        }

        using JsonDocument document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    private static void WriteWithMetadataLast(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            IEnumerable<JsonProperty> properties = value.EnumerateObject();
            foreach (JsonProperty property in properties.Where(property => !property.Name.StartsWith('$'))
                .Concat(properties.Where(property => property.Name.StartsWith('$'))))
            {
                writer.WritePropertyName(property.Name);
                WriteWithMetadataLast(writer, property.Value);
            }

            writer.WriteEndObject();
            return;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (JsonElement item in value.EnumerateArray())
            {
                WriteWithMetadataLast(writer, item);
            }

            writer.WriteEndArray();
            return;
        }

        value.WriteTo(writer);
    }

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
            OwnerUserId = Guid.CreateVersion7(),
            CreatedTime = now,
            UpdatedTime = now,
            PublishedTime = now,
        };
    }

    private sealed class StubLLMProvider(LLMModel model) : ILLMProvider
    {
        public LLMProviderManagementInfo GetManagementInfo() => new();

        public Task<IReadOnlyList<LLMModel>> ListModelsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LLMModel>>([model]);

        public Task<LLMModel> GetModelAsync(string modelId, CancellationToken cancellationToken = default) =>
            Task.FromResult(model);

        public Task<LLMModelTestResult> TestModelAsync(
            string modelId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubChatLLMProvider : IChatLLMProvider, IDisposable
    {
        private static readonly HttpClient client = new()
        {
            BaseAddress = new Uri("https://litellm.example/"),
        };

        private LiteLLMProvider? _provider;

        public int CallCount { get; private set; }

        public string? ModelId { get; private set; }

        public IChatClient CreateChatClient(string modelId)
        {
            this.CallCount++;
            this.ModelId = modelId;
            this._provider = new LiteLLMProvider(
                client,
                Options.Create(new LiteLLMOptions
                {
                    Endpoint = new Uri("https://litellm.example/"),
                    ApiKey = "test-key",
                }));
            return this._provider.CreateChatClient(modelId);
        }

        public void Dispose() => this._provider?.Dispose();
    }

    private sealed class RespondingChatLLMProvider : IChatLLMProvider
    {
        public IChatClient CreateChatClient(string modelId) => new RespondingChatClient(modelId);
    }

    private sealed class CapturingChatLLMProvider : IChatLLMProvider
    {
        private readonly CapturingChatClient _client = new();

        public ChatOptions? LastOptions => this._client.LastOptions;

        public IChatClient CreateChatClient(string modelId) => this._client;
    }

    private sealed class CapturingChatClient : IChatClient
    {
        public ChatOptions? LastOptions { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            this.LastOptions = options;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            this.LastOptions = options;
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "done");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }

    private sealed class RespondingChatClient(string modelId) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "done"))
            {
                ModelId = modelId,
                Usage = new UsageDetails
                {
                    InputTokenCount = 1,
                    OutputTokenCount = 1,
                    TotalTokenCount = 2,
                },
            });

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "done");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }

    private sealed class StubAgentToolCatalogService(AgentToolDefinition? tool) : IAgentToolCatalogService
    {
        public Task<IReadOnlyList<AgentToolDefinition>> ListAvailableToolsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentToolDefinition>>(tool is null ? [] : [tool]);

        public Task<AgentToolDefinition> GetToolAsync(Guid toolId, CancellationToken ct = default) =>
            Task.FromResult(tool is not null && toolId == tool.Id ? tool : throw new KeyNotFoundException());

        public Task ValidateToolBindingAsync(Guid toolId, string? parametersJson, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubPersistenceProvider : IPersistenceProvider
    {
        private readonly StubMessageRepository _messages = new();
        private readonly StubConversationRepository _conversations = new();

        public TRepository GetRepository<TRepository>() where TRepository : notnull
        {
            if (this._messages is TRepository messages)
            {
                return messages;
            }

            if (this._conversations is TRepository conversations)
            {
                return conversations;
            }

            throw new NotSupportedException();
        }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubMessageRepository : IAgentChatMessageRepository
    {
        public Task<PagedResult<AgentChatMessage>> ListMessagesByConversation(Guid conversationId, Pagination pagination, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ChatMessage>> ListHistoryMessagesAsync(Guid conversationId, int? maxMessages = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentChatMessage>> ListAllMessagesByConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentChatMessage>> ListMessagesByRun(Guid conversationId, string runId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentChatMessage>> AddMessages(IReadOnlyList<AgentChatMessage> messages, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<int> DeleteMessagesByConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubConversationRepository : IAgentConversationRepository
    {
        public Task<AgentConversation> AddConversation(AgentConversation conversation, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<AgentConversation> GetConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<AgentConversation> GetConversationBySessionKey(string sessionKey, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<PagedResult<AgentConversationListItem>> ListConversations(Guid agentId, Guid ownerUserId, Pagination pagination, CancellationToken ct = default) => throw new NotSupportedException();

        public Task UpdateConversation(AgentConversation conversation, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> DeleteConversation(Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
