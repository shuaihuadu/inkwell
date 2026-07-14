// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Claims;
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
        Guid ownerUserId = Guid.CreateVersion7();
        AgentVersion firstVersion = CreateVersion(agentId, 1);
        AgentVersion secondVersion = CreateVersion(agentId, 2);
        StubAgentVersionService versions = new(CreateDefinition(agentId, ownerUserId, firstVersion.Id), firstVersion, secondVersion);
        RecordingAgentFactory factory = new();
        ServiceCollection services = new();
        services.AddSingleton<IAgentVersionService>(versions);
        services.AddSingleton<IAgentFactory>(factory);
        services.AddSingleton<IAgentToolBindingResolver, EmptyToolBindingResolver>();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues["agentId"] = agentId.ToString();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, ownerUserId.ToString())],
            "test"));
        HttpContextAccessor accessor = new() { HttpContext = httpContext };
        RoutingAgent agent = new(accessor, serviceProvider.GetRequiredService<IServiceScopeFactory>());

        // Act
        AgentSession session = await agent.CreateSessionAsync();
        versions.Definition = CreateDefinition(agentId, ownerUserId, secondVersion.Id);
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

    private static AgentDefinition CreateDefinition(Guid agentId, Guid ownerUserId, Guid publishedVersionId) => new()
    {
        Id = agentId,
        OwnerUserId = ownerUserId,
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

    private sealed class StubAgentVersionService(
        AgentDefinition definition,
        params AgentVersion[] versions) : IAgentVersionService
    {
        public AgentDefinition Definition { get; set; } = definition;

        private readonly Dictionary<Guid, AgentVersion> _versions = versions.ToDictionary(version => version.Id);

        public Task<AgentVersion> GetPublishedVersionAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._versions[this.Definition.CurrentPublishedVersionId!.Value]);

        public Task<AgentVersion> GetPublishedVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._versions[versionId]);

        public Task<AgentVersion> GetVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._versions[versionId]);

        public Task<IReadOnlyList<AgentVersion>> ListVersionsAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AgentVersion> SaveDraftAsync(Guid agentId, AgentSnapshot snapshot, Guid actorUserId, string? changeSummary = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AgentVersion> PublishDraftAsync(Guid agentId, Guid actorUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AgentVersion> RollbackAsync(Guid agentId, Guid sourceVersionId, Guid actorUserId, string? changeSummary = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
