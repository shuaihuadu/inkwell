// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.AgentRuntime;

/// <summary>
/// 验证已发布 Agent 的构建用例编排。
/// </summary>
[TestClass]
public sealed class AgentBuildServiceTests
{
    /// <summary>
    /// 验证构建服务按调用者加载不可变版本并交给 Factory。
    /// </summary>
    [TestMethod]
    public async Task BuildPublishedAsync_ValidRequest_DelegatesResolvedVersionAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        Guid requestingUserId = Guid.CreateVersion7();
        AgentVersion version = new()
        {
            Id = Guid.CreateVersion7(),
            AgentId = agentId,
            VersionNumber = 1,
            Snapshot = CreateSnapshot(),
            CreatedByUserId = Guid.CreateVersion7(),
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow,
            PublishedTime = DateTimeOffset.UtcNow,
        };
        RecordingVersionService versionService = new(version);
        RecordingAgentService agentService = new();
        RecordingAgentFactory factory = new();
        AgentBuildService service = new(versionService, agentService, factory);

        // Act
        AIAgent result = await service.BuildPublishedAsync(agentId, requestingUserId);

        // Assert
        Assert.AreSame(factory.Agent, result);
        Assert.AreEqual(agentId, versionService.AgentId);
        Assert.AreEqual(requestingUserId, versionService.RequestingUserId);
        Assert.AreSame(version, factory.Version);
    }

    /// <summary>
    /// 验证已发布 Agent 试运行禁用需要产品会话的持久化聊天历史。
    /// </summary>
    [TestMethod]
    public async Task BuildPublishedTrialAsync_WithChatHistory_DisablesChatHistoryAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        Guid requestingUserId = Guid.CreateVersion7();
        AgentVersion version = new()
        {
            Id = Guid.CreateVersion7(),
            AgentId = agentId,
            VersionNumber = 1,
            Snapshot = CreateSnapshot() with
            {
                BuildOptions = CreateBuildOptionsWithChatHistory(),
            },
            CreatedByUserId = Guid.CreateVersion7(),
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow,
            PublishedTime = DateTimeOffset.UtcNow,
        };
        RecordingAgentFactory factory = new();
        AgentBuildService service = new(new RecordingVersionService(version), new RecordingAgentService(), factory);

        // Act
        _ = await service.BuildPublishedTrialAsync(agentId, requestingUserId);

        // Assert
        Assert.IsNotNull(version.Snapshot.BuildOptions.ChatHistoryOptions);
        Assert.IsNotNull(factory.Version);
        Assert.IsNull(factory.Version.Snapshot.BuildOptions.ChatHistoryOptions);
    }

    /// <summary>
    /// 验证产品会话按锁定版本构建，并选择外部持久化聊天历史。
    /// </summary>
    [TestMethod]
    public async Task BuildPublishedConversationAsync_WithChatHistory_UsesBoundVersionAndPersistentHistoryAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        Guid requestingUserId = Guid.CreateVersion7();
        AgentVersion version = new()
        {
            Id = versionId,
            AgentId = agentId,
            VersionNumber = 1,
            Snapshot = CreateSnapshot() with
            {
                BuildOptions = CreateBuildOptionsWithChatHistory(),
            },
            CreatedByUserId = Guid.CreateVersion7(),
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow,
            PublishedTime = DateTimeOffset.UtcNow,
        };
        RecordingVersionService versionService = new(version);
        RecordingAgentFactory factory = new();
        AgentBuildService service = new(versionService, new RecordingAgentService(), factory);

        // Act
        _ = await service.BuildPublishedConversationAsync(agentId, versionId, requestingUserId);

        // Assert
        Assert.AreEqual(versionId, versionService.VersionId);
        Assert.AreEqual(requestingUserId, versionService.RequestingUserId);
        Assert.IsNotNull(version.Snapshot.BuildOptions.ChatHistoryOptions);
        Assert.AreSame(version, factory.ConversationVersion);
    }

    /// <summary>
    /// 验证草稿构建只加载所有者当前保存的 Agent 定义。
    /// </summary>
    [TestMethod]
    public async Task BuildDraftAsync_OwnerRequest_DelegatesAgentDefinitionAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId);
        RecordingVersionService versionService = new(null);
        RecordingAgentService agentService = new(agent);
        RecordingAgentFactory factory = new();
        AgentBuildService service = new(versionService, agentService, factory);

        // Act
        AIAgent result = await service.BuildDraftAsync(agent.Id, ownerUserId);

        // Assert
        Assert.AreSame(factory.Agent, result);
        Assert.AreEqual(agent.Id, agentService.AgentId);
        Assert.AreEqual(ownerUserId, agentService.RequestingUserId);
        Assert.AreSame(agent, factory.Definition);
    }

    /// <summary>
    /// 验证草稿试运行禁用需要产品会话的持久化聊天历史。
    /// </summary>
    [TestMethod]
    public async Task BuildDraftTrialAsync_WithChatHistory_DisablesChatHistoryAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId) with
        {
            BuildOptions = CreateBuildOptionsWithChatHistory(),
        };
        RecordingAgentFactory factory = new();
        AgentBuildService service = new(new RecordingVersionService(null), new RecordingAgentService(agent), factory);

        // Act
        _ = await service.BuildDraftTrialAsync(agent.Id, ownerUserId);

        // Assert
        Assert.IsNotNull(agent.BuildOptions.ChatHistoryOptions);
        Assert.IsNotNull(factory.Definition);
        Assert.IsNull(factory.Definition.BuildOptions.ChatHistoryOptions);
    }

    /// <summary>
    /// 验证普通草稿构建仍保留持久化聊天历史配置。
    /// </summary>
    [TestMethod]
    public async Task BuildDraftAsync_WithChatHistory_PreservesChatHistoryAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId) with
        {
            BuildOptions = CreateBuildOptionsWithChatHistory(),
        };
        RecordingAgentFactory factory = new();
        AgentBuildService service = new(new RecordingVersionService(null), new RecordingAgentService(agent), factory);

        // Act
        _ = await service.BuildDraftAsync(agent.Id, ownerUserId);

        // Assert
        Assert.IsNotNull(factory.Definition?.BuildOptions.ChatHistoryOptions);
    }

    /// <summary>
    /// 验证共享 Agent 的非所有者不能构建草稿。
    /// </summary>
    [TestMethod]
    public async Task BuildDraftAsync_NonOwnerRequest_ThrowsUnauthorizedAccessExceptionAsync()
    {
        // Arrange
        AgentDefinition agent = CreateAgent(Guid.CreateVersion7()) with { IsShared = true };
        RecordingVersionService versionService = new(null);
        RecordingAgentService agentService = new(agent);
        RecordingAgentFactory factory = new();
        AgentBuildService service = new(versionService, agentService, factory);

        // Act
        ValueTask<AIAgent> action = service.BuildDraftAsync(agent.Id, Guid.CreateVersion7());

        // Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(action.AsTask);
        Assert.IsNull(factory.Definition);
    }

    private sealed class RecordingVersionService(AgentVersion? version) : IAgentVersionService
    {
        public Guid AgentId { get; private set; }

        public Guid RequestingUserId { get; private set; }

        public Guid VersionId { get; private set; }

        public Task<AgentVersion> GetPublishedVersionAsync(
            Guid agentId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default)
        {
            this.AgentId = agentId;
            this.RequestingUserId = requestingUserId;
            return Task.FromResult(version ?? throw new InvalidOperationException("No published version was configured."));
        }

        public Task<AgentVersion> GetPublishedVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default)
        {
            this.AgentId = agentId;
            this.VersionId = versionId;
            this.RequestingUserId = requestingUserId;
            return Task.FromResult(version ?? throw new InvalidOperationException("No published version was configured."));
        }

        public Task<AgentVersion> GetVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AgentVersion>> ListVersionsAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AgentVersion> PublishAsync(Guid agentId, Guid actorUserId, string? changeSummary = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AgentVersion> RollbackAsync(Guid agentId, Guid sourceVersionId, Guid actorUserId, string? changeSummary = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingAgentService(AgentDefinition? agent = null) : IAgentService
    {
        public Guid AgentId { get; private set; }

        public Guid RequestingUserId { get; private set; }

        public Task<AgentDefinition> GetAgentAsync(Guid agentId, Guid requestingUserId, CancellationToken ct = default)
        {
            this.AgentId = agentId;
            this.RequestingUserId = requestingUserId;
            return Task.FromResult(agent ?? throw new InvalidOperationException("No Agent definition was configured."));
        }

        public Task<AgentDefinition> CreateAgentAsync(AgentUpsertRequest request, Guid ownerUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<AgentDefinition> UpdateAgentAsync(Guid agentId, AgentUpsertRequest request, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AgentListItem>> ListMyAgentsAsync(Guid ownerUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AgentListItem>> ListSharedAgentsAsync(Guid excludingOwnerUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task ShareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task UnshareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task RevokeShareAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<AgentDefinition> CloneAgentAsync(Guid agentId, Guid newOwnerUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingAgentFactory : IAgentFactory
    {
        public AIAgent Agent { get; } = new StubAgent();

        public AgentVersion? Version { get; private set; }

        public AgentVersion? ConversationVersion { get; private set; }

        public AgentDefinition? Definition { get; private set; }

        public ValueTask<AIAgent> BuildAsync(AgentDefinition agent, CancellationToken cancellationToken = default)
        {
            this.Definition = agent;
            return ValueTask.FromResult(this.Agent);
        }

        public ValueTask<AIAgent> BuildAsync(AgentVersion version, CancellationToken cancellationToken = default)
        {
            this.Version = version;
            return ValueTask.FromResult(this.Agent);
        }

        public ValueTask<AIAgent> BuildConversationAsync(AgentVersion version, CancellationToken cancellationToken = default)
        {
            this.ConversationVersion = version;
            return ValueTask.FromResult(this.Agent);
        }
    }

    private static AgentSnapshot CreateSnapshot() => new()
    {
        Name = "assistant",
        BuildOptions = new AgentBuildOptions
        {
            ModelOptions = new AgentModelOptions { ModelId = "test-model" },
        },
    };

    private static AgentBuildOptions CreateBuildOptionsWithChatHistory() => new()
    {
        ModelOptions = new AgentModelOptions { ModelId = "test-model" },
        ChatHistoryOptions = new AgentChatHistoryOptions { MaxMessages = 40 },
    };

    private static AgentDefinition CreateAgent(Guid ownerUserId) => new()
    {
        Id = Guid.CreateVersion7(),
        OwnerUserId = ownerUserId,
        Name = "assistant",
        BuildOptions = new AgentBuildOptions
        {
            ModelOptions = new AgentModelOptions { ModelId = "test-model" },
        },
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private sealed class StubAgent : AIAgent
    {
        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        protected override Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
