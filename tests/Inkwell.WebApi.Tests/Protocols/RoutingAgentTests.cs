// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Protocols;

namespace Inkwell.WebApi.Tests.Protocols;

/// <summary>
/// 验证动态协议 Agent 的版本路由与 Session 恢复行为。
/// </summary>
[TestClass]
public sealed class RoutingAgentTests
{
    /// <summary>
    /// 验证已有 Session 在新版本发布后仍按创建时版本序列化和恢复。
    /// </summary>
    [TestMethod]
    public async Task SessionAsync_AfterNewPublish_RemainsOnOriginalVersionAsync()
    {
        // Arrange
        Guid agentId = Guid.CreateVersion7();
        AgentVersion firstVersion = CreateVersion(agentId, 1);
        AgentVersion secondVersion = CreateVersion(agentId, 2);
        StubAgentRepository agents = new(CreateDefinition(agentId, firstVersion.Id));
        StubAgentVersionRepository versions = new(firstVersion, secondVersion);
        RecordingAgentFactory factory = new();
        ServiceCollection services = new();
        services.AddSingleton<IAgentRepository>(agents);
        services.AddSingleton<IAgentVersionRepository>(versions);
        services.AddSingleton<IAgentFactory>(factory);
        services.AddSingleton<IAgentToolBindingResolver, EmptyToolBindingResolver>();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues["agentId"] = agentId.ToString();
        HttpContextAccessor accessor = new() { HttpContext = httpContext };
        RoutingAgent agent = new(accessor, serviceProvider.GetRequiredService<IServiceScopeFactory>());

        // Act
        AgentSession session = await agent.CreateSessionAsync();
        agents.Definition = CreateDefinition(agentId, secondVersion.Id);
        JsonElement serializedState = await agent.SerializeSessionAsync(session);
        AgentSession restoredSession = await agent.DeserializeSessionAsync(serializedState);

        // Assert
        RoutingAgentSession routedSession = (RoutingAgentSession)restoredSession;
        Assert.AreEqual(firstVersion.Id, routedSession.AgentVersionId);
        CollectionAssert.AreEqual(
            new[] { firstVersion.Id, firstVersion.Id, firstVersion.Id },
            factory.BuiltVersionIds);
        CollectionAssert.DoesNotContain(factory.BuiltVersionIds, secondVersion.Id);
    }

    private static AgentDefinition CreateDefinition(Guid agentId, Guid publishedVersionId) => new()
    {
        Id = agentId,
        OwnerUserId = Guid.CreateVersion7(),
        CurrentPublishedVersionId = publishedVersionId,
        LatestPublishedVersionNumber = 1,
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private static AgentVersion CreateVersion(Guid agentId, int versionNumber) => new()
    {
        Id = Guid.CreateVersion7(),
        AgentId = agentId,
        VersionNumber = versionNumber,
        Status = AgentVersionStatus.Published,
        Snapshot = new AgentSnapshot { Name = $"agent-v{versionNumber}" },
        CreatedByUserId = Guid.CreateVersion7(),
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
        PublishedTime = DateTimeOffset.UtcNow,
    };

    private sealed class EmptyToolBindingResolver : IAgentToolBindingResolver
    {
        public Task<IReadOnlyList<AIFunction>> ResolveAsync(IReadOnlyList<AgentToolBinding> bindings, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AIFunction>>([]);
    }

    private sealed class RecordingAgentFactory : IAgentFactory
    {
        public List<Guid> BuiltVersionIds { get; } = [];

        public ValueTask<AIAgent> BuildAsync(
            AgentVersion agentVersion,
            AgentBuildOptions agentBuildOptions,
            CancellationToken cancellationToken = default)
        {
            this.BuiltVersionIds.Add(agentVersion.Id);
            return ValueTask.FromResult<AIAgent>(new StubAgent());
        }
    }

    private sealed class StubAgent : AIAgent
    {
        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AgentSession>(new StubAgentSession());

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(JsonSerializer.SerializeToElement(new { state = "stub" }, jsonSerializerOptions));

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AgentSession>(new StubAgentSession());

        protected override Task<Microsoft.Agents.AI.AgentResponse> RunCoreAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            Microsoft.Agents.AI.AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Microsoft.Agents.AI.AgentResponse());

        protected override IAsyncEnumerable<Microsoft.Agents.AI.AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            Microsoft.Agents.AI.AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<Microsoft.Agents.AI.AgentResponseUpdate>();
    }

    private sealed class StubAgentSession : AgentSession;

    private sealed class StubAgentRepository(AgentDefinition definition) : IAgentRepository
    {
        public AgentDefinition Definition { get; set; } = definition;

        public Task<AgentDefinition> AddAgent(AgentDefinition agent, CancellationToken ct = default) => throw new NotSupportedException();

        public Task UpdateAgent(AgentDefinition agent, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<AgentDefinition> GetAgent(Guid id, CancellationToken ct = default) => Task.FromResult(this.Definition);

        public Task<bool> DeleteAgent(Guid id, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<PagedResult<AgentDefinition>> ListAgents(Pagination pagination, SortOrder sort, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentDefinition>> FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<AgentDefinition>> FindSharedAgents(Guid excludingOwnerUserId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubAgentVersionRepository(params AgentVersion[] versions) : IAgentVersionRepository
    {
        private readonly Dictionary<Guid, AgentVersion> _versions = versions.ToDictionary(version => version.Id);

        public Task<AgentVersion> AddVersionAsync(AgentVersion version, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<AgentVersion> UpdateVersionAsync(AgentVersion version, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<AgentVersion> GetVersionAsync(Guid versionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._versions[versionId]);

        public Task<IReadOnlyList<AgentVersion>> ListVersionsByAgentAsync(Guid agentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, AgentVersion>> FindVersionsByIdsAsync(IReadOnlyList<Guid> versionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}