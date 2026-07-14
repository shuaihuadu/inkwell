// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.Versioning;

/// <summary>
/// 验证 Agent 草稿、发布、可见性和回滚生命周期。
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
        Assert.AreEqual(AgentVersionStatus.Published, resolved.Status);
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
    /// 验证即使 Owner 可见草稿，也不能把草稿解析为可调用发布版本。
    /// </summary>
    [TestMethod]
    public async Task GetPublishedVersionAsync_ForDraftVersion_ThrowsInvalidOperationAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId);
        AgentVersion draft = CreateDraft(agent, ownerUserId, "draft");
        agent = agent with { DraftVersionId = draft.Id };
        AgentVersionService service = CreateService(agent, draft);

        // Act
        Task ActAsync() => service.GetPublishedVersionAsync(agent.Id, draft.Id, ownerUserId);

        // Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(ActAsync);
    }

    /// <summary>
    /// 验证发布草稿会更新聚合指针并保留不可变快照。
    /// </summary>
    [TestMethod]
    public async Task PublishDraftAsync_WithExistingDraft_PublishesAndUpdatesAgentAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId);
        AgentVersion draft = CreateDraft(agent, ownerUserId, "draft");
        agent = agent with { DraftVersionId = draft.Id };
        InMemoryAgentRepository agents = new(agent);
        InMemoryAgentVersionRepository versions = new(draft);
        AgentVersionService service = new(new ImmediatePersistenceProvider(agents, versions));

        // Act
        AgentVersion published = await service.PublishDraftAsync(agent.Id, ownerUserId);

        // Assert
        AgentDefinition updatedAgent = await agents.GetAgent(agent.Id);
        Assert.AreEqual(AgentVersionStatus.Published, published.Status);
        Assert.AreEqual(1, published.VersionNumber);
        Assert.IsNotNull(published.PublishedTime);
        Assert.AreEqual(published.Id, updatedAgent.CurrentPublishedVersionId);
        Assert.IsNull(updatedAgent.DraftVersionId);
        Assert.AreEqual(1, updatedAgent.LatestPublishedVersionNumber);
    }

    /// <summary>
    /// 验证共享用户只能看到已发布版本，不能看到 Owner 草稿。
    /// </summary>
    [TestMethod]
    public async Task ListVersionsAsync_ForSharedViewer_ExcludesDraftAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        Guid viewerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId) with { IsShared = true, LatestPublishedVersionNumber = 1 };
        AgentVersion published = CreatePublished(agent, ownerUserId, 1, "published");
        AgentVersion draft = CreateDraft(agent, ownerUserId, "draft") with { VersionNumber = 2 };
        agent = agent with { CurrentPublishedVersionId = published.Id, DraftVersionId = draft.Id };
        AgentVersionService service = CreateService(agent, published, draft);

        // Act
        IReadOnlyList<AgentVersion> visibleVersions = await service.ListVersionsAsync(agent.Id, viewerUserId);

        // Assert
        Assert.HasCount(1, visibleVersions);
        Assert.AreEqual(published.Id, visibleVersions[0].Id);
        Assert.AreEqual(AgentVersionStatus.Published, visibleVersions[0].Status);
    }

    /// <summary>
    /// 验证回滚会复制历史快照形成新版本，而不会修改历史版本。
    /// </summary>
    [TestMethod]
    public async Task RollbackAsync_FromPublishedVersion_CreatesNewPublishedVersionAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId) with { LatestPublishedVersionNumber = 2 };
        AgentVersion versionOne = CreatePublished(agent, ownerUserId, 1, "version-one");
        AgentVersion versionTwo = CreatePublished(agent, ownerUserId, 2, "version-two");
        agent = agent with { CurrentPublishedVersionId = versionTwo.Id };
        InMemoryAgentRepository agents = new(agent);
        InMemoryAgentVersionRepository versions = new(versionOne, versionTwo);
        AgentVersionService service = new(new ImmediatePersistenceProvider(agents, versions));

        // Act
        AgentVersion rollback = await service.RollbackAsync(agent.Id, versionOne.Id, ownerUserId);

        // Assert
        AgentDefinition updatedAgent = await agents.GetAgent(agent.Id);
        AgentVersion originalVersion = await versions.GetVersionAsync(versionOne.Id);
        Assert.AreNotEqual(versionOne.Id, rollback.Id);
        Assert.AreEqual(3, rollback.VersionNumber);
        Assert.AreEqual("version-one", rollback.Snapshot.Instructions);
        Assert.AreEqual("version-one", originalVersion.Snapshot.Instructions);
        Assert.AreEqual(rollback.Id, updatedAgent.CurrentPublishedVersionId);
        Assert.AreEqual(3, updatedAgent.LatestPublishedVersionNumber);
    }

    /// <summary>
    /// 验证保存草稿只替换草稿快照，不生成额外版本。
    /// </summary>
    [TestMethod]
    public async Task SaveDraftAsync_WithExistingDraft_ReplacesSnapshotAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        AgentDefinition agent = CreateAgent(ownerUserId);
        AgentVersion draft = CreateDraft(agent, ownerUserId, "old");
        agent = agent with { DraftVersionId = draft.Id };
        InMemoryAgentVersionRepository versions = new(draft);
        AgentVersionService service = new(new ImmediatePersistenceProvider(new InMemoryAgentRepository(agent), versions));
        AgentSnapshot updatedSnapshot = CreateSnapshot("new");

        // Act
        AgentVersion updatedDraft = await service.SaveDraftAsync(agent.Id, updatedSnapshot, ownerUserId, "updated draft");

        // Assert
        IReadOnlyList<AgentVersion> allVersions = await versions.ListVersionsByAgentAsync(agent.Id);
        Assert.HasCount(1, allVersions);
        Assert.AreEqual(draft.Id, updatedDraft.Id);
        Assert.AreEqual(AgentVersionStatus.Draft, updatedDraft.Status);
        Assert.AreEqual("new", updatedDraft.Snapshot.Instructions);
        Assert.IsNull(updatedDraft.PublishedTime);
    }

    private static AgentDefinition CreateAgent(Guid ownerUserId) => new()
    {
        Id = Guid.CreateVersion7(),
        OwnerUserId = ownerUserId,
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private static AgentVersion CreateDraft(AgentDefinition agent, Guid ownerUserId, string instructions) => new()
    {
        Id = Guid.CreateVersion7(),
        AgentId = agent.Id,
        VersionNumber = agent.LatestPublishedVersionNumber + 1,
        Status = AgentVersionStatus.Draft,
        Snapshot = CreateSnapshot(instructions),
        CreatedByUserId = ownerUserId,
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private static AgentVersion CreatePublished(AgentDefinition agent, Guid ownerUserId, int versionNumber, string instructions)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return new AgentVersion
        {
            Id = Guid.CreateVersion7(),
            AgentId = agent.Id,
            VersionNumber = versionNumber,
            Status = AgentVersionStatus.Published,
            Snapshot = CreateSnapshot(instructions),
            CreatedByUserId = ownerUserId,
            CreatedTime = now,
            UpdatedTime = now,
            PublishedTime = now,
        };
    }

    private static AgentSnapshot CreateSnapshot(string instructions) => new()
    {
        Name = "Versioned agent",
        Instructions = instructions,
        ModelId = "test-model",
    };

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