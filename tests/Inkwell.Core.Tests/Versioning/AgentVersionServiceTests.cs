// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.Versioning;

/// <summary>
/// 验证 Agent 发布版本、可见性和回滚生命周期。
/// </summary>
[TestClass]
public sealed class AgentVersionServiceTests
{
    /// <summary>
    /// 验证共享用户可以解析当前发布版本。
    /// </summary>
    [TestMethod]
    public async Task GetPublishedVersionAsync_ForSharedViewer_ReturnsPublishedVersionAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        Guid viewerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId) with { IsShared = true };
        AgentVersion published = CreatePublished(agent, ownerUserId, 1, "published");
        agent = agent with { CurrentPublishedVersionId = published.Id, LatestPublishedVersionNumber = 1 };
        AgentVersionService service = CreateService(agent, published);

        // Act
        AgentVersion resolved = await service.GetPublishedVersionAsync(agent.Id, viewerUserId);

        // Assert
        Assert.AreEqual(published.Id, resolved.Id);
        Assert.AreEqual(agent.Id, resolved.AgentId);
    }

    /// <summary>
    /// 验证非 Owner 不能解析私有 Agent 的发布版本。
    /// </summary>
    [TestMethod]
    public async Task GetPublishedVersionAsync_ForPrivateViewer_ThrowsUnauthorizedAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        Guid viewerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId);
        AgentVersion published = CreatePublished(agent, ownerUserId, 1, "published");
        agent = agent with { CurrentPublishedVersionId = published.Id, LatestPublishedVersionNumber = 1 };
        AgentVersionService service = CreateService(agent, published);

        // Act
        Task ActAsync() => service.GetPublishedVersionAsync(agent.Id, viewerUserId);

        // Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(ActAsync);
    }

    /// <summary>
    /// 验证发布当前 Agent 会创建自包含快照并更新版本指针。
    /// </summary>
    [TestMethod]
    public async Task PublishAsync_WithCurrentAgent_CreatesImmutableSnapshotAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId);
        InMemoryAgentRepository agents = new(agent);
        InMemoryAgentVersionRepository versions = new();
        AgentVersionService service = new(new ImmediatePersistenceProvider(agents, versions));

        // Act
        AgentVersion published = await service.PublishAsync(agent.Id, ownerUserId, "Update model settings");

        // Assert
        AgentDefinition updatedAgent = await agents.GetAgent(agent.Id);
        Assert.AreEqual(agent.Id, published.AgentId);
        Assert.AreEqual(agent.Name, published.Snapshot.Name);
        Assert.AreSame(agent.BuildOptions, published.Snapshot.BuildOptions);
        Assert.AreEqual("Update model settings", published.ChangeSummary);
        Assert.AreEqual(published.Id, updatedAgent.CurrentPublishedVersionId);
        Assert.AreEqual(1, updatedAgent.LatestPublishedVersionNumber);
        Assert.IsNotNull(published.PublishedTime);
    }

    /// <summary>
    /// 验证回滚会创建新的发布快照并把历史内容恢复为当前 Agent 定义。
    /// </summary>
    [TestMethod]
    public async Task RollbackAsync_FromPublishedVersion_CreatesVersionAndRestoresAgentAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId) with { LatestPublishedVersionNumber = 2 };
        AgentVersion versionOne = CreatePublished(agent, ownerUserId, 1, "version-one");
        AgentVersion versionTwo = CreatePublished(agent with { Instructions = "version-two" }, ownerUserId, 2, "version-two");
        agent = agent with { CurrentPublishedVersionId = versionTwo.Id, Instructions = "current-edit" };
        InMemoryAgentRepository agents = new(agent);
        InMemoryAgentVersionRepository versions = new(versionOne, versionTwo);
        AgentVersionService service = new(new ImmediatePersistenceProvider(agents, versions));

        // Act
        AgentVersion rollback = await service.RollbackAsync(agent.Id, versionOne.Id, ownerUserId);

        // Assert
        AgentDefinition updatedAgent = await agents.GetAgent(agent.Id);
        Assert.AreNotEqual(versionOne.Id, rollback.Id);
        Assert.AreEqual(agent.Id, rollback.AgentId);
        Assert.AreEqual(3, rollback.VersionNumber);
        Assert.AreEqual("version-one", rollback.Snapshot.Instructions);
        Assert.AreEqual("version-one", updatedAgent.Instructions);
        Assert.AreEqual(rollback.Id, updatedAgent.CurrentPublishedVersionId);
        Assert.AreEqual(3, updatedAgent.LatestPublishedVersionNumber);
    }

    private static AgentDefinition CreateAgent(Guid ownerUserId) => new()
    {
        Id = Guid.CreateVersion7(),
        OwnerUserId = ownerUserId,
        Name = "Versioned agent",
        Instructions = "current",
        BuildOptions = new AgentBuildOptions
        {
            ModelOptions = new AgentModelOptions { ModelId = "test-model" },
        },
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private static AgentVersion CreatePublished(AgentDefinition agent, Guid ownerUserId, int versionNumber, string instructions)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid versionId = Guid.CreateVersion7();

        return new AgentVersion
        {
            Id = versionId,
            AgentId = agent.Id,
            VersionNumber = versionNumber,
            Snapshot = new AgentSnapshot
            {
                Name = agent.Name,
                AvatarUri = agent.AvatarUri,
                Description = agent.Description,
                Instructions = instructions,
                BuildOptions = agent.BuildOptions,
            },
            CreatedByUserId = ownerUserId,
            CreatedTime = now,
            UpdatedTime = now,
            PublishedTime = now,
        };
    }

    private static AgentVersionService CreateService(AgentDefinition agent, params AgentVersion[] versions) =>
        new(new ImmediatePersistenceProvider(
            new InMemoryAgentRepository(agent),
            new InMemoryAgentVersionRepository(versions)));

    private sealed class ImmediatePersistenceProvider(params object[] repositories) : IPersistenceProvider
    {
        private readonly Dictionary<Type, object> _repositories = repositories.ToDictionary(repository => repository.GetType().GetInterfaces().Single());

        public TRepository GetRepository<TRepository>() where TRepository : notnull =>
            (TRepository)this._repositories[typeof(TRepository)];

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            await action(ct).ConfigureAwait(false);

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            await action(ct).ConfigureAwait(false);

        public async Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            await action(ct).ConfigureAwait(false);

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            await action(ct).ConfigureAwait(false);
    }

    private sealed class InMemoryAgentRepository(params AgentDefinition[] initialAgents) : IAgentRepository
    {
        private readonly Dictionary<Guid, AgentDefinition> _agents = initialAgents.ToDictionary(agent => agent.Id);

        public Task<AgentDefinition> AddAgent(AgentDefinition agent, CancellationToken ct = default)
        {
            this._agents.Add(agent.Id, agent);
            return Task.FromResult(agent);
        }

        public Task UpdateAgent(AgentDefinition agent, CancellationToken ct = default)
        {
            this._agents[agent.Id] = agent;
            return Task.CompletedTask;
        }

        public Task<AgentDefinition> GetAgent(Guid id, CancellationToken ct = default) =>
            Task.FromResult(this._agents.TryGetValue(id, out AgentDefinition? agent)
                ? agent
                : throw new KeyNotFoundException());

        public Task<bool> DeleteAgent(Guid id, CancellationToken ct = default) => Task.FromResult(this._agents.Remove(id));

        public Task<PagedResult<AgentDefinition>> ListAgents(Pagination pagination, SortOrder sort, CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<AgentDefinition>([.. this._agents.Values], this._agents.Count, pagination));

        public Task<IReadOnlyList<AgentDefinition>> FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentDefinition>>([.. this._agents.Values.Where(agent => agent.OwnerUserId == ownerUserId)]);

        public Task<IReadOnlyList<AgentDefinition>> FindSharedAgents(Guid excludingOwnerUserId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentDefinition>>([.. this._agents.Values.Where(agent => agent.IsShared && agent.OwnerUserId != excludingOwnerUserId)]);
    }

    private sealed class InMemoryAgentVersionRepository(params AgentVersion[] initialVersions) : IAgentVersionRepository
    {
        private readonly Dictionary<Guid, AgentVersion> _versions = initialVersions.ToDictionary(version => version.Id);

        public Task<AgentVersion> AddVersionAsync(AgentVersion version, CancellationToken cancellationToken = default)
        {
            this._versions.Add(version.Id, version);
            return Task.FromResult(version);
        }

        public Task<AgentVersion> UpdateVersionAsync(AgentVersion version, CancellationToken cancellationToken = default)
        {
            this._versions[version.Id] = version;
            return Task.FromResult(version);
        }

        public Task<AgentVersion> GetVersionAsync(Guid versionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._versions.TryGetValue(versionId, out AgentVersion? version)
                ? version
                : throw new KeyNotFoundException());

        public Task<IReadOnlyList<AgentVersion>> ListVersionsByAgentAsync(Guid agentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentVersion>>([.. this._versions.Values
                .Where(version => version.AgentId == agentId)
                .OrderByDescending(version => version.VersionNumber)]);

        public Task<IReadOnlyDictionary<Guid, AgentVersion>> FindVersionsByIdsAsync(IReadOnlyList<Guid> versionIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, AgentVersion>>(this._versions
                .Where(pair => versionIds.Contains(pair.Key))
                .ToDictionary());
    }
}
