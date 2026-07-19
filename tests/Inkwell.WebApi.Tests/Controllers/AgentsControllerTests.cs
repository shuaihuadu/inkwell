// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Claims;
using Inkwell.WebApi.Agents;
using Inkwell.WebApi.Controllers;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>
/// 验证 Agent 管理 API 的头像上传边界。
/// </summary>
[TestClass]
public sealed class AgentsControllerTests
{
    /// <summary>
    /// 验证合法头像写入文件存储并返回持久 URI。
    /// </summary>
    [TestMethod]
    public async Task UploadAvatarAsync_WithPng_StoresOwnedAvatarAsync()
    {
        // Arrange
        Guid userId = Guid.CreateVersion7();
        RecordingFileStorageProvider storage = new();
        AgentsController controller = CreateController(userId, storage);
        byte[] content = [0x89, 0x50, 0x4E, 0x47];
        FormFile file = new(new MemoryStream(content), 0, content.Length, "file", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };

        // Act
        ActionResult<AgentAvatarUploadResponse> result = await controller.UploadAvatarAsync(file, CancellationToken.None);
        ObjectResult created = (ObjectResult)result.Result!;
        AgentAvatarUploadResponse response = (AgentAvatarUploadResponse)created.Value!;

        // Assert
        Assert.AreEqual(StatusCodes.Status201Created, created.StatusCode);
        Assert.AreEqual("agent-avatars", storage.Container);
        StringAssert.StartsWith(storage.Key, $"{userId:N}/");
        StringAssert.EndsWith(storage.Key, ".png");
        Assert.AreEqual("image/png", storage.Metadata?.ContentType);
        Assert.AreEqual($"inkwell://agent-avatars/{storage.Key}", response.AvatarUri.ToString());
    }

    /// <summary>
    /// 验证非图片文件不会写入文件存储。
    /// </summary>
    [TestMethod]
    public async Task UploadAvatarAsync_WithUnsupportedContentType_RejectsFileAsync()
    {
        // Arrange
        RecordingFileStorageProvider storage = new();
        AgentsController controller = CreateController(Guid.CreateVersion7(), storage);
        byte[] content = "not-an-image"u8.ToArray();
        FormFile file = new(new MemoryStream(content), 0, content.Length, "file", "avatar.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain",
        };

        // Act
        Task ActAsync() => controller.UploadAvatarAsync(file, CancellationToken.None);

        // Assert
        await Assert.ThrowsExactlyAsync<ArgumentException>(ActAsync);
        Assert.IsNull(storage.Container);
    }

    /// <summary>
    /// 验证头像读取返回存储中的图片流和内容类型。
    /// </summary>
    [TestMethod]
    public async Task DownloadAvatarAsync_WithValidKey_ReturnsStoredImageAsync()
    {
        // Arrange
        RecordingFileStorageProvider storage = new();
        AgentsController controller = CreateController(Guid.CreateVersion7(), storage);

        // Act
        IActionResult result = await controller.DownloadAvatarAsync("owner/avatar.webp", CancellationToken.None);
        FileStreamResult file = (FileStreamResult)result;

        // Assert
        Assert.AreEqual("agent-avatars", storage.Container);
        Assert.AreEqual("owner/avatar.webp", storage.Key);
        Assert.AreEqual("image/webp", file.ContentType);
    }

    private static AgentsController CreateController(Guid userId, RecordingFileStorageProvider storage)
    {
        ClaimsIdentity identity = new(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Test");
        AgentsController controller = new(new RejectingAgentService(), storage)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            },
        };
        return controller;
    }

    private sealed class RejectingAgentService : IAgentService
    {
        public Task<AgentDefinition> CreateAgentAsync(AgentUpsertRequest request, Guid ownerUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AgentDefinition> UpdateAgentAsync(Guid agentId, AgentUpsertRequest request, Guid actorUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> DeleteAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AgentDefinition> GetAgentAsync(Guid agentId, Guid requestingUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AgentListItem>> ListMyAgentsAsync(Guid ownerUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AgentListItem>> ListSharedAgentsAsync(Guid excludingOwnerUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ShareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UnshareAgentAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task RevokeShareAsync(Guid agentId, Guid actorUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AgentDefinition> CloneAgentAsync(Guid agentId, Guid newOwnerUserId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class RecordingFileStorageProvider : IFileStorageProvider
    {
        public string? Container { get; private set; }
        public string Key { get; private set; } = string.Empty;
        public FileMetadata? Metadata { get; private set; }

        public Task<FileUploadResult> UploadAsync(string container, string key, Stream content, FileMetadata metadata, CancellationToken ct = default)
        {
            this.Container = container;
            this.Key = key;
            this.Metadata = metadata;
            return Task.FromResult(new FileUploadResult(container, key, content.Length, "etag", DateTimeOffset.UtcNow));
        }

        public Task<FileDownloadResponse> DownloadAsync(string container, string key, CancellationToken ct = default)
        {
            this.Container = container;
            this.Key = key;
            return Task.FromResult(new FileDownloadResponse(
                new MemoryStream([0x52, 0x49, 0x46, 0x46]),
                new FileMetadata("image/webp"),
                "etag",
                4,
                DateTimeOffset.UtcNow));
        }
        public Task<bool> ExistsAsync(string container, string key, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(string container, string key, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Uri> CreatePresignedUploadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Uri> CreatePresignedDownloadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default) => throw new NotSupportedException();

        public async IAsyncEnumerable<FileObjectInfo> ListAsync(string container, string? prefix = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
