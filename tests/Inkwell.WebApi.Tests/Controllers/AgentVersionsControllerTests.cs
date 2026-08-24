// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Claims;
using Inkwell.WebApi.Agents;
using Inkwell.WebApi.Controllers;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>
/// 验证 Agent 版本 API 的调用者身份与请求映射。
/// </summary>
[TestClass]
public sealed class AgentVersionsControllerTests
{
    /// <summary>
    /// 验证发布请求把当前用户与变更摘要传给版本服务。
    /// </summary>
    [TestMethod]
    public async Task PublishAsync_WithAuthenticatedUser_ForwardsRequestAsync()
    {
        // Arrange
        Guid userId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        RecordingAgentVersionService service = new();
        AgentVersionsController controller = CreateController(userId, service);

        // Act
        ActionResult<AgentVersion> result = await controller.PublishAsync(
            agentId,
            new PublishAgentVersionRequest("Release notes"),
            CancellationToken.None);

        // Assert
        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.AreEqual(service.Result, ok.Value);
        Assert.AreEqual(agentId, service.AgentId);
        Assert.AreEqual(userId, service.UserId);
        Assert.AreEqual("Release notes", service.ChangeSummary);
    }

    /// <summary>
    /// 验证回滚请求把来源版本和当前用户传给版本服务。
    /// </summary>
    [TestMethod]
    public async Task RollbackAsync_WithAuthenticatedUser_ForwardsRequestAsync()
    {
        // Arrange
        Guid userId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        Guid sourceVersionId = Guid.CreateVersion7();
        RecordingAgentVersionService service = new();
        AgentVersionsController controller = CreateController(userId, service);

        // Act
        ActionResult<AgentVersion> result = await controller.RollbackAsync(
            agentId,
            sourceVersionId,
            new RollbackAgentVersionRequest("Restore stable configuration"),
            CancellationToken.None);

        // Assert
        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.AreEqual(service.Result, ok.Value);
        Assert.AreEqual(agentId, service.AgentId);
        Assert.AreEqual(sourceVersionId, service.SourceVersionId);
        Assert.AreEqual(userId, service.UserId);
        Assert.AreEqual("Restore stable configuration", service.ChangeSummary);
    }

    private static AgentVersionsController CreateController(Guid userId, RecordingAgentVersionService service)
    {
        ClaimsIdentity identity = new([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test");
        return new AgentVersionsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            },
        };
    }

    private sealed class RecordingAgentVersionService : IAgentVersionService
    {
        public AgentVersion Result { get; } = CreateVersion();
        public Guid AgentId { get; private set; }
        public Guid? SourceVersionId { get; private set; }
        public Guid UserId { get; private set; }
        public string? ChangeSummary { get; private set; }

        public Task<AgentVersion> PublishAsync(Guid agentId, Guid actorUserId, string? changeSummary = null, CancellationToken cancellationToken = default)
        {
            this.AgentId = agentId;
            this.UserId = actorUserId;
            this.ChangeSummary = changeSummary;
            return Task.FromResult(this.Result);
        }

        public Task<AgentVersion> RollbackAsync(Guid agentId, Guid sourceVersionId, Guid actorUserId, string? changeSummary = null, CancellationToken cancellationToken = default)
        {
            this.AgentId = agentId;
            this.SourceVersionId = sourceVersionId;
            this.UserId = actorUserId;
            this.ChangeSummary = changeSummary;
            return Task.FromResult(this.Result);
        }

        public Task<AgentVersion> GetPublishedVersionAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AgentVersion> GetPublishedVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AgentVersion> GetVersionAsync(Guid agentId, Guid versionId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AgentVersion>> ListVersionsAsync(Guid agentId, Guid requestingUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        private static AgentVersion CreateVersion()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return new AgentVersion
            {
                Id = Guid.CreateVersion7(),
                AgentId = Guid.CreateVersion7(),
                VersionNumber = 1,
                Snapshot = new AgentSnapshot
                {
                    Name = "Versioned agent",
                    BuildOptions = new AgentBuildOptions
                    {
                        ModelOptions = new AgentModelOptions { ModelId = "test-model" },
                    },
                },
                OwnerUserId = Guid.CreateVersion7(),
                CreatedTime = now,
                UpdatedTime = now,
                PublishedTime = now,
            };
        }
    }
}
