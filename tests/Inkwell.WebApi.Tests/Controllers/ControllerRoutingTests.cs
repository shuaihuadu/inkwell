// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>
/// 验证业务 API Controller 的路由和授权边界。
/// </summary>
[TestClass]
public sealed class ControllerRoutingTests
{
    /// <summary>
    /// 验证 MVC 实际发现的业务 API 路由与迁移前保持一致。
    /// </summary>
    [TestMethod]
    public void Controllers_RegisterExpectedBusinessApiRoutes()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        using WebApplication application = builder.Build();

        // Act
        application.MapControllers();
        IEndpointRouteBuilder endpoints = application;
        string?[] routePatterns =
        [
            .. endpoints.DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .Select(endpoint => endpoint.RoutePattern.RawText),
        ];

        // Assert
        CollectionAssert.Contains(routePatterns, "api/auth/login");
        CollectionAssert.Contains(routePatterns, "api/auth/logout");
        CollectionAssert.Contains(routePatterns, "api/auth/session");
        CollectionAssert.Contains(routePatterns, "api/agents");
        CollectionAssert.Contains(routePatterns, "api/agents/mine");
        CollectionAssert.Contains(routePatterns, "api/agents/shared");
        CollectionAssert.Contains(routePatterns, "api/agents/{agentId:guid}");
        CollectionAssert.Contains(routePatterns, "api/agents/{agentId:guid}/share");
        CollectionAssert.Contains(routePatterns, "api/agents/{agentId:guid}/clone");
        CollectionAssert.Contains(routePatterns, "api/agents/{agentId:guid}/versions");
        CollectionAssert.Contains(routePatterns, "api/agents/{agentId:guid}/versions/{versionId:guid}");
        CollectionAssert.Contains(routePatterns, "api/agents/{agentId:guid}/draft");
        CollectionAssert.Contains(routePatterns, "api/agents/{agentId:guid}/publish");
        CollectionAssert.Contains(routePatterns, "api/agents/{agentId:guid}/versions/{versionId:guid}/rollback");
    }

    /// <summary>
    /// 验证登录匿名开放，而 Auth Controller 的其余操作要求有效登录态。
    /// </summary>
    [TestMethod]
    public void AuthController_DefinesExpectedAuthorization()
    {
        // Arrange
        Type controllerType = typeof(AuthController);

        // Act
        AuthorizeAttribute? authorization = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();
        AllowAnonymousAttribute? allowAnonymous = typeof(AuthController)
            .GetMethod(nameof(AuthController.LoginAsync))?
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .Cast<AllowAnonymousAttribute>()
            .SingleOrDefault();

        // Assert
        Assert.AreEqual(AuthorizationPolicies.RequireAuthenticatedUser, authorization?.Policy);
        Assert.IsNotNull(allowAnonymous);
        Assert.AreEqual("api/auth", controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>().Single().Template);
    }

    /// <summary>
    /// 验证 Agent 管理与版本 Controller 使用预期路由并要求有效登录态。
    /// </summary>
    [TestMethod]
    public void AgentControllers_DefineExpectedRoutesAndAuthorization()
    {
        // Arrange
        Type agentsController = typeof(AgentsController);
        Type versionsController = typeof(AgentVersionsController);

        // Act
        string? agentsRoute = agentsController.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>().Single().Template;
        string? versionsRoute = versionsController.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>().Single().Template;
        string? agentsPolicy = agentsController.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>().Single().Policy;
        string? versionsPolicy = versionsController.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>().Single().Policy;

        // Assert
        Assert.AreEqual("api/agents", agentsRoute);
        Assert.AreEqual("api/agents/{agentId:guid}", versionsRoute);
        Assert.AreEqual(AuthorizationPolicies.RequireAuthenticatedUser, agentsPolicy);
        Assert.AreEqual(AuthorizationPolicies.RequireAuthenticatedUser, versionsPolicy);
    }
}
