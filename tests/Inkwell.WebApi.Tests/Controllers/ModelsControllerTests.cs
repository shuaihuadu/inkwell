// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Reflection;
using Inkwell.WebApi.Controllers;
using Microsoft.AspNetCore.RateLimiting;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>
/// 验证实时模型 API 的返回形状与授权边界。
/// </summary>
[TestClass]
public sealed class ModelsControllerTests
{
    /// <summary>
    /// 验证模型列表直接返回 Provider 的实时分类与能力。
    /// </summary>
    [TestMethod]
    public async Task ListAsync_ReturnsProviderModelsAsync()
    {
        // Arrange
        LLMModel chatModel = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        LLMModel embeddingModel = CreateModel("text-embedding-3-large", LLMModelCategory.Embedding);
        ModelsController controller = new(new StubLLMProvider([chatModel, embeddingModel]));

        // Act
        ActionResult<IReadOnlyList<LLMModel>> result = await controller.ListAsync(CancellationToken.None);
        OkObjectResult okResult = (OkObjectResult)result.Result!;
        IReadOnlyList<LLMModel> models = (IReadOnlyList<LLMModel>)okResult.Value!;

        // Assert
        Assert.HasCount(2, models);
        Assert.AreSame(chatModel, models[0]);
        Assert.AreSame(embeddingModel, models[1]);
    }

    /// <summary>
    /// 验证模型详情返回 Provider 解析后的模型。
    /// </summary>
    [TestMethod]
    public async Task GetAsync_ReturnsProviderModelAsync()
    {
        // Arrange
        LLMModel model = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        ModelsController controller = new(new StubLLMProvider([model]));

        // Act
        ActionResult<LLMModel> result = await controller.GetAsync(model.Id, CancellationToken.None);
        OkObjectResult okResult = (OkObjectResult)result.Result!;

        // Assert
        Assert.AreSame(model, okResult.Value);
    }

    /// <summary>
    /// 验证模型测试返回脱敏后的 Provider 测试结果。
    /// </summary>
    [TestMethod]
    public async Task TestAsync_ReturnsProviderTestResultAsync()
    {
        // Arrange
        LLMModel model = CreateModel("gpt-5.4", LLMModelCategory.Chat);
        StubLLMProvider provider = new([model]);
        ModelsController controller = new(provider);

        // Act
        ActionResult<LLMModelTestResult> result = await controller.TestAsync(model.Id, CancellationToken.None);
        OkObjectResult okResult = (OkObjectResult)result.Result!;
        LLMModelTestResult testResult = (LLMModelTestResult)okResult.Value!;

        // Assert
        Assert.IsTrue(testResult.IsSuccess);
        Assert.AreEqual(model.Id, testResult.ModelId);
    }

    /// <summary>
    /// 验证管理元数据由 Provider 返回且不暴露私有连接配置。
    /// </summary>
    [TestMethod]
    public void GetManagementInfo_ReturnsProviderDashboardUrl()
    {
        // Arrange
        ModelsController controller = new(new StubLLMProvider([]));

        // Act
        ActionResult<LLMProviderManagementInfo> result = controller.GetManagementInfo();
        OkObjectResult okResult = (OkObjectResult)result.Result!;
        LLMProviderManagementInfo managementInfo = (LLMProviderManagementInfo)okResult.Value!;

        // Assert
        Assert.AreEqual(new Uri("https://litellm.example/"), managementInfo.DashboardUrl);
    }

    /// <summary>
    /// 验证模型 API 要求登录，模型测试端点使用独立的用户级限流。
    /// </summary>
    [TestMethod]
    public void Controller_DefinesExpectedAuthorizationPolicies()
    {
        // Arrange
        Type controllerType = typeof(ModelsController);
        MethodInfo testMethod = controllerType.GetMethod(nameof(ModelsController.TestAsync))!;

        // Act
        string? controllerPolicy = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single()
            .Policy;
        EnableRateLimitingAttribute rateLimiting = testMethod
            .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>()
            .Single();

        // Assert
        Assert.AreEqual(AuthorizationPolicies.RequireAuthenticatedUser, controllerPolicy);
        Assert.IsEmpty(testMethod.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        Assert.AreEqual(AuthorizationPolicies.ModelTestRateLimiterPolicy, rateLimiting.PolicyName);
    }

    private static LLMModel CreateModel(string id, LLMModelCategory category) => new()
    {
        Id = id,
        Category = category,
        ProviderMode = category == LLMModelCategory.Chat ? "chat" : "embedding",
        OwnedBy = "openai",
        SupportsTools = category == LLMModelCategory.Chat,
    };

    private sealed class StubLLMProvider(IReadOnlyList<LLMModel> models) : ILLMProvider
    {
        public LLMProviderManagementInfo GetManagementInfo() => new()
        {
            DashboardUrl = new Uri("https://litellm.example/"),
        };

        public Task<IReadOnlyList<LLMModel>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(models);
        }

        public Task<LLMModel> GetModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LLMModel model = models.Single(item => string.Equals(item.Id, modelId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(model);
        }

        public Task<LLMModelTestResult> TestModelAsync(
            string modelId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new LLMModelTestResult
            {
                ModelId = modelId,
                IsSuccess = true,
                Latency = TimeSpan.FromMilliseconds(10),
            });
        }
    }
}
