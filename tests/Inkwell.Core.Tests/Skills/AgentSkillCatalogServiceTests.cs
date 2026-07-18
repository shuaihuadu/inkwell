// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.Skills;

/// <summary>
/// 验证 Skill 上传、所有权和维护规则。
/// </summary>
[TestClass]
public sealed class AgentSkillCatalogServiceTests
{
    /// <summary>
    /// 验证上传含脚本的 Skill 时保存脚本引用但不执行。
    /// </summary>
    [TestMethod]
    public async Task UploadSkillAsync_WithScripts_PreservesScriptUrisAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        Uri scriptUri = new("inkwell://skills/package/scripts/run.ps1");
        InMemoryAgentSkillRepository repository = new();
        AgentSkillCatalogService service = CreateService(repository);
        AgentSkillUploadRequest request = new()
        {
            SkillMdContent = """
                ---
                name: contract-review
                description: Reviews contracts.
                ---
                # Contract review
                """,
            PackageEntries = [new AgentSkillPackageEntry("scripts/run.ps1", scriptUri)],
        };

        // Act
        AgentSkillDefinition result = await service.UploadSkillAsync(request, ownerUserId);

        // Assert
        Assert.AreEqual(ownerUserId, result.OwnerUserId);
        Assert.HasCount(1, result.ScriptFileUris);
        Assert.AreEqual(scriptUri, result.ScriptFileUris[0]);
    }

    /// <summary>
    /// 验证 Owner 更新可编辑字段时保留只读资源。
    /// </summary>
    [TestMethod]
    public async Task UpdateSkillAsync_ByOwner_UpdatesContentAndPreservesResourcesAsync()
    {
        // Arrange
        Guid ownerUserId = Guid.CreateVersion7();
        Uri referenceUri = new("inkwell://skills/package/references/rule.md");
        AgentSkillDefinition existing = CreateSkill(ownerUserId) with
        {
            ReferenceFileUris = [referenceUri],
        };
        InMemoryAgentSkillRepository repository = new(existing);
        AgentSkillCatalogService service = CreateService(repository);
        AgentSkillUpdateRequest request = new(
            "updated-name",
            "Updated description.",
            "# Updated");

        // Act
        AgentSkillDefinition result = await service.UpdateSkillAsync(
            existing.Id,
            request,
            ownerUserId,
            actorIsAdmin: false);

        // Assert
        Assert.AreEqual("updated-name", result.Name);
        Assert.AreEqual("# Updated", result.Content);
        Assert.HasCount(1, result.ReferenceFileUris);
        Assert.AreEqual(referenceUri, result.ReferenceFileUris[0]);
    }

    /// <summary>
    /// 验证普通成员不能更新其他用户的 Skill。
    /// </summary>
    [TestMethod]
    public async Task UpdateSkillAsync_ByOtherMember_ThrowsUnauthorizedAccessExceptionAsync()
    {
        // Arrange
        AgentSkillDefinition existing = CreateSkill(Guid.CreateVersion7());
        InMemoryAgentSkillRepository repository = new(existing);
        AgentSkillCatalogService service = CreateService(repository);
        AgentSkillUpdateRequest request = new(
            existing.Name,
            existing.Description,
            existing.Content);

        // Act
        Task ActAsync() => service.UpdateSkillAsync(
            existing.Id,
            request,
            Guid.CreateVersion7(),
            actorIsAdmin: false);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(ActAsync);
    }

    private static AgentSkillCatalogService CreateService(
        IAgentSkillRepository repository) =>
        new(new StubPersistenceProvider(repository));

    private static AgentSkillDefinition CreateSkill(Guid ownerUserId) => new()
    {
        Id = Guid.CreateVersion7(),
        OwnerUserId = ownerUserId,
        Name = "contract-review",
        Description = "Reviews contracts.",
        Content = "# Contract review",
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private sealed class InMemoryAgentSkillRepository(params AgentSkillDefinition[] initial)
        : IAgentSkillRepository
    {
        private readonly Dictionary<Guid, AgentSkillDefinition> _skills =
            initial.ToDictionary(skill => skill.Id);

        public Task<AgentSkillDefinition> AddSkill(
            AgentSkillDefinition skill,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            this._skills.Add(skill.Id, skill);
            return Task.FromResult(skill);
        }

        public Task<AgentSkillDefinition> UpdateSkill(
            AgentSkillDefinition skill,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            this._skills[skill.Id] = skill;
            return Task.FromResult(skill);
        }

        public Task<bool> DeleteSkill(Guid id, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(this._skills.Remove(id));
        }

        public Task<AgentSkillDefinition> GetSkill(Guid id, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(
                this._skills.TryGetValue(id, out AgentSkillDefinition? skill)
                    ? skill
                    : throw new KeyNotFoundException());
        }

        public Task<PagedResult<AgentSkillDefinition>> ListSkills(
            Pagination pagination,
            SortOrder sort,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<AgentSkillDefinition> items = [.. this._skills.Values];
            return Task.FromResult(new PagedResult<AgentSkillDefinition>(items, items.Count, pagination));
        }
    }

    private sealed class StubPersistenceProvider(IAgentSkillRepository repository)
        : IPersistenceProvider
    {
        public TRepository GetRepository<TRepository>() where TRepository : notnull =>
            repository is TRepository typed
                ? typed
                : throw new NotSupportedException();

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken ct = default) => action(ct);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action,
            CancellationToken ct = default) => action(ct);

        public Task ExecuteInTransactionAsync(
            IsolationLevel isolationLevel,
            Func<CancellationToken, Task> action,
            CancellationToken ct = default) => action(ct);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            IsolationLevel isolationLevel,
            Func<CancellationToken, Task<TResult>> action,
            CancellationToken ct = default) => action(ct);
    }
}
