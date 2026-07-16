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
        RecordingAgentFactory factory = new();
        AgentBuildService service = new(versionService, factory);

        // Act
        AIAgent result = await service.BuildPublishedAsync(agentId, requestingUserId);

        // Assert
        Assert.AreSame(factory.Agent, result);
        Assert.AreEqual(agentId, versionService.AgentId);
        Assert.AreEqual(requestingUserId, versionService.RequestingUserId);
        Assert.AreSame(version, factory.Version);
    }

    private sealed class RecordingVersionService(AgentVersion version) : IAgentVersionService
    {
        public Guid AgentId { get; private set; }

        public Guid RequestingUserId { get; private set; }

        public Task<AgentVersion> GetPublishedVersionAsync(
            Guid agentId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default)
        {
            this.AgentId = agentId;
            this.RequestingUserId = requestingUserId;
            return Task.FromResult(version);
        }

        public Task<AgentVersion> GetPublishedVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AgentVersion> GetVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AgentVersion>> ListVersionsAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AgentVersion> PublishAsync(Guid agentId, Guid actorUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AgentVersion> RollbackAsync(Guid agentId, Guid sourceVersionId, Guid actorUserId, string? changeSummary = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingAgentFactory : IAgentFactory
    {
        public AIAgent Agent { get; } = new StubAgent();

        public AgentVersion? Version { get; private set; }

        public ValueTask<AIAgent> BuildAsync(AgentDefinition agent, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<AIAgent> BuildAsync(AgentVersion version, CancellationToken cancellationToken = default)
        {
            this.Version = version;
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