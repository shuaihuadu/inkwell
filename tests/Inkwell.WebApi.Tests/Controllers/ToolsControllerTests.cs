// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Reflection;
using Inkwell.WebApi.Controllers;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>
/// 验证只读工具目录 API 的返回形状与授权边界。
/// </summary>
[TestClass]
public sealed class ToolsControllerTests
{
    /// <summary>
    /// 验证工具列表直接返回目录服务结果。
    /// </summary>
    [TestMethod]
    public async Task ListAsync_ReturnsCatalogToolsAsync()
    {
        // Arrange
        AgentToolDefinition tool = CreateTool();
        ToolsController controller = new(new StubAgentToolCatalogService([tool]));

        // Act
        ActionResult<IReadOnlyList<AgentToolDefinition>> result = await controller
            .ListAsync(CancellationToken.None);
        OkObjectResult okResult = (OkObjectResult)result.Result!;
        IReadOnlyList<AgentToolDefinition> tools =
            (IReadOnlyList<AgentToolDefinition>)okResult.Value!;

        // Assert
        Assert.HasCount(1, tools);
        Assert.AreSame(tool, tools[0]);
    }

    /// <summary>
    /// 验证工具目录要求登录且不提供写操作。
    /// </summary>
    [TestMethod]
    public void Controller_DefinesAuthenticatedReadOnlyContract()
    {
        // Arrange
        Type controllerType = typeof(ToolsController);

        // Act
        string? policy = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single()
            .Policy;
        string[] publicMethods = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();

        // Assert
        Assert.AreEqual(AuthorizationPolicies.RequireAuthenticatedUser, policy);
        CollectionAssert.AreEquivalent(new[] { nameof(ToolsController.ListAsync) }, publicMethods);
    }

    private static AgentToolDefinition CreateTool() => new()
    {
        Id = Guid.Parse("0198a96d-19e4-7000-8000-000000000101"),
        Name = "current_date_time",
        Description = "Returns the current date and time.",
        ParametersJsonSchema = """{"type":"object","properties":{"timeZone":{"type":"string"}}}""",
        CreatedTime = DateTimeOffset.Parse("2026-07-18T00:00:00Z"),
        UpdatedTime = DateTimeOffset.Parse("2026-07-18T00:00:00Z"),
    };

    private sealed class StubAgentToolCatalogService(IReadOnlyList<AgentToolDefinition> tools)
        : IAgentToolCatalogService
    {
        public Task<IReadOnlyList<AgentToolDefinition>> ListAvailableToolsAsync(
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(tools);
        }

        public Task<AgentToolDefinition> GetToolAsync(
            Guid toolId,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(tools.Single(tool => tool.Id == toolId));
        }

        public Task ValidateToolBindingAsync(
            Guid toolId,
            string? parametersJson,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}