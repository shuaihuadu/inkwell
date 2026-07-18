// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Claims;
using Inkwell.WebApi.Controllers;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>
/// 验证 Skill 管理 API 的身份传递和上传响应。
/// </summary>
[TestClass]
public sealed class SkillsControllerTests
{
    /// <summary>
    /// 验证 Markdown 上传使用当前用户作为 Owner 并返回资源地址。
    /// </summary>
    [TestMethod]
    public async Task UploadAsync_WithMarkdown_CreatesOwnedSkillAsync()
    {
        // Arrange
        Guid userId = Guid.CreateVersion7();
        StubAgentSkillCatalogService service = new(userId);
        SkillsController controller = CreateController(service, userId, isSuper: false);
        byte[] content = """
            ---
            name: contract-review
            description: Reviews contracts.
            ---
            # Contract review
            """u8.ToArray();
        FormFile file = new(new MemoryStream(content), 0, content.Length, "file", "SKILL.md");

        // Act
        ActionResult<AgentSkillDefinition> result = await controller.UploadAsync(
            file,
            CancellationToken.None);
        CreatedResult created = (CreatedResult)result.Result!;
        AgentSkillDefinition skill = (AgentSkillDefinition)created.Value!;

        // Assert
        Assert.AreEqual(userId, service.UploadOwnerUserId);
        Assert.AreEqual($"/api/skills/{skill.Id}", created.Location);
        Assert.AreEqual("contract-review", skill.Name);
    }

    /// <summary>
    /// 验证更新 API 将当前用户和管理员标记传给业务服务。
    /// </summary>
    [TestMethod]
    public async Task UpdateAsync_AsAdministrator_ForwardsActorContextAsync()
    {
        // Arrange
        Guid userId = Guid.CreateVersion7();
        StubAgentSkillCatalogService service = new(userId);
        SkillsController controller = CreateController(service, userId, isSuper: true);
        AgentSkillUpdateRequest request = new(
            "updated",
            "Updated description.",
            "# Updated");

        // Act
        _ = await controller.UpdateAsync(Guid.CreateVersion7(), request, CancellationToken.None);

        // Assert
        Assert.AreEqual(userId, service.UpdateActorUserId);
        Assert.IsTrue(service.UpdateActorIsSuper);
    }

    private static SkillsController CreateController(
        StubAgentSkillCatalogService service,
        Guid userId,
        bool isSuper)
    {
        ClaimsIdentity identity = new(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(SessionClaimTypes.IsSuper, isSuper.ToString()),
            ],
            "Test");
        SkillsController controller = new(service, new RejectingFileStorageProvider())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            },
        };
        return controller;
    }

    private sealed class StubAgentSkillCatalogService(Guid ownerUserId)
        : IAgentSkillCatalogService
    {
        public Guid? UploadOwnerUserId { get; private set; }

        public Guid? UpdateActorUserId { get; private set; }

        public bool UpdateActorIsSuper { get; private set; }

        public Task<IReadOnlyList<AgentSkillDefinition>> ListAvailableSkillsAsync(
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentSkillDefinition>>([]);

        public Task<AgentSkillDefinition> GetSkillAsync(
            Guid skillId,
            CancellationToken ct = default) =>
            Task.FromResult(this.CreateSkill(skillId));

        public Task<AgentSkillDefinition> UploadSkillAsync(
            AgentSkillUploadRequest request,
            Guid uploadedOwnerUserId,
            CancellationToken ct = default)
        {
            this.UploadOwnerUserId = uploadedOwnerUserId;
            return Task.FromResult(this.CreateSkill(Guid.CreateVersion7()));
        }

        public Task<AgentSkillDefinition> UpdateSkillAsync(
            Guid skillId,
            AgentSkillUpdateRequest request,
            Guid actorUserId,
            bool actorIsSuper,
            CancellationToken ct = default)
        {
            this.UpdateActorUserId = actorUserId;
            this.UpdateActorIsSuper = actorIsSuper;
            return Task.FromResult(this.CreateSkill(skillId));
        }

        public Task<bool> DeleteSkillAsync(
            Guid skillId,
            Guid actorUserId,
            bool actorIsSuper,
            CancellationToken ct = default) => Task.FromResult(true);

        private AgentSkillDefinition CreateSkill(Guid id) => new()
        {
            Id = id,
            OwnerUserId = ownerUserId,
            Name = "contract-review",
            Description = "Reviews contracts.",
            Content = "# Contract review",
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow,
        };
    }

    private sealed class RejectingFileStorageProvider : IFileStorageProvider
    {
        public Task<FileUploadResult> UploadAsync(
            string container,
            string key,
            Stream content,
            FileMetadata metadata,
            CancellationToken ct = default) => throw new AssertFailedException("Markdown upload must not use file storage.");

        public Task<FileDownloadResponse> DownloadAsync(string container, string key, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsAsync(string container, string key, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string container, string key, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Uri> CreatePresignedUploadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Uri> CreatePresignedDownloadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<FileObjectInfo> ListAsync(
            string container,
            string? prefix = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
