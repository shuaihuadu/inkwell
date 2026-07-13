// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>
/// 验证模型注册表 API 的返回形状与授权边界。
/// </summary>
[TestClass]
public sealed class ModelsControllerTests
{
    /// <summary>
    /// 验证模型列表包含可用和不可用模型，并保留产品元数据。
    /// </summary>
    [TestMethod]
    public async Task ListAsync_ReturnsAllRegistryModelsAsync()
    {
        // Arrange
        ModelDefinition availableModel = CreateModel("qwen-plus", isAvailable: true);
        ModelDefinition unavailableModel = CreateModel("new-model", isAvailable: false);
        ModelsController controller = new(new StubModelRegistryService([availableModel, unavailableModel]));

        // Act
        ActionResult<IReadOnlyList<ModelDefinition>> result = await controller.ListAsync(CancellationToken.None);
        OkObjectResult okResult = (OkObjectResult)result.Result!;
        IReadOnlyList<ModelDefinition> models = (IReadOnlyList<ModelDefinition>)okResult.Value!;

        // Assert
        Assert.HasCount(2, models);
        Assert.AreSame(availableModel, models[0]);
        Assert.AreSame(unavailableModel, models[1]);
        Assert.AreEqual("Model metadata is incomplete.", models[1].UnavailableReason);
    }

    /// <summary>
    /// 验证模型详情返回 Registry 解析后的同一模型定义。
    /// </summary>
    [TestMethod]
    public async Task GetAsync_ReturnsRegistryModelAsync()
    {
        // Arrange
        ModelDefinition model = CreateModel("qwen-plus", isAvailable: true);
        ModelsController controller = new(new StubModelRegistryService([model]));

        // Act
        ActionResult<ModelDefinition> result = await controller.GetAsync(model.Id, CancellationToken.None);
        OkObjectResult okResult = (OkObjectResult)result.Result!;

        // Assert
        Assert.AreSame(model, okResult.Value);
    }

    /// <summary>
    /// 验证模型 API 使用预期路由并要求有效登录态。
    /// </summary>
    [TestMethod]
    public void Controller_DefinesExpectedRouteAndAuthorization()
    {
        // Arrange
        Type controllerType = typeof(ModelsController);

        // Act
        string? route = controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Single()
            .Template;
        string? policy = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single()
            .Policy;

        // Assert
        Assert.AreEqual("api/models", route);
        Assert.AreEqual(AuthorizationPolicies.RequireAuthenticatedUser, policy);
    }

    private static ModelDefinition CreateModel(string id, bool isAvailable) => new()
    {
        Id = id,
        DisplayName = id,
        PublisherId = "alibaba",
        PublisherDisplayName = "Alibaba Cloud",
        FamilyId = "qwen",
        FamilyDisplayName = "Qwen",
        SourceId = "litellm",
        RuntimeId = "litellm",
        RemoteModelId = id,
        SupportsTools = true,
        IsAvailable = isAvailable,
        UnavailableReason = isAvailable ? null : "Model metadata is incomplete.",
    };

    private sealed class StubModelRegistryService(IReadOnlyList<ModelDefinition> models) : IModelRegistryService
    {
        public Task<IReadOnlyList<ModelDefinition>> ListModelsAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(models);
        }

        public Task<ModelDefinition> GetModelAsync(string modelId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            ModelDefinition model = models.Single(item => string.Equals(item.Id, modelId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(model);
        }
    }
}
