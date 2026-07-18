// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Controllers;
using Inkwell.WebApi.Conversations;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>
/// 验证业务 API Controller 的路由和授权边界。
/// </summary>
[TestClass]
public sealed class ControllerRoutingTests
{
    /// <summary>
    /// 验证 MVC 实际发现的业务 API 路由符合预期。
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
        Assert.Contains("api/auth/login", routePatterns);
        Assert.Contains("api/auth/logout", routePatterns);
        Assert.Contains("api/auth/session", routePatterns);
        Assert.Contains("api/agents", routePatterns);
        Assert.Contains("api/agents/mine", routePatterns);
        Assert.Contains("api/agents/shared", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/share", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/share/revoke", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/clone", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/versions", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/versions/{versionId:guid}", routePatterns);
        Assert.DoesNotContain("api/agents/{agentId:guid}/draft", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/publish", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/versions/{versionId:guid}/rollback", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/conversations", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/conversations/{conversationId:guid}/messages", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/conversations/{conversationId:guid}/messages/{messageId:guid}", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/conversations/{conversationId:guid}/clear", routePatterns);
        Assert.Contains("api/agents/{agentId:guid}/conversations/{conversationId:guid}", routePatterns);
    }

    /// <summary>
    /// 验证登录匿名开放，而 Auth Controller 的其余操作要求基础登录态，并提供改密入口。
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
        HttpPostAttribute? changePasswordRoute = typeof(AuthController)
            .GetMethod(nameof(AuthController.ChangePasswordAsync))?
            .GetCustomAttributes(typeof(HttpPostAttribute), inherit: true)
            .Cast<HttpPostAttribute>()
            .SingleOrDefault();

        // Assert
        Assert.IsNotNull(authorization);
        Assert.IsNull(authorization.Policy);
        Assert.IsNotNull(allowAnonymous);
        Assert.AreEqual("password", changePasswordRoute?.Template);
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
        string? revokeSharePolicy = agentsController
            .GetMethod(nameof(AgentsController.RevokeShareAsync))?
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single()
            .Policy;

        // Assert
        Assert.AreEqual("api/agents", agentsRoute);
        Assert.AreEqual("api/agents/{agentId:guid}", versionsRoute);
        Assert.AreEqual(AuthorizationPolicies.RequireAuthenticatedUser, agentsPolicy);
        Assert.AreEqual(AuthorizationPolicies.RequireAuthenticatedUser, versionsPolicy);
        Assert.AreEqual(AuthorizationPolicies.RequireAdmin, revokeSharePolicy);
    }

    /// <summary>
    /// 验证 Conversation Controller 要求有效登录态且响应不泄漏内部持久化字段。
    /// </summary>
    [TestMethod]
    public void ConversationController_DefinesAuthorizationAndSafeResponseBoundary()
    {
        // Arrange
        Type controllerType = typeof(AgentConversationsController);
        string[] forbiddenPropertyNames =
        [
            "SessionKey",
            "OwnerUserId",
            "LastCommittedRunId",
            "SerializedState",
        ];

        // Act
        string? route = controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>().Single().Template;
        string? policy = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>().Single().Policy;
        string[] responsePropertyNames =
        [
            .. typeof(AgentConversationResponse).GetProperties().Select(property => property.Name),
            .. typeof(AgentConversationListItem).GetProperties().Select(property => property.Name),
            .. typeof(AgentChatMessageResponse).GetProperties().Select(property => property.Name),
        ];

        // Assert
        Assert.AreEqual("api/agents/{agentId:guid}/conversations", route);
        Assert.AreEqual(AuthorizationPolicies.RequireAuthenticatedUser, policy);
        foreach (string forbiddenPropertyName in forbiddenPropertyNames)
        {
            Assert.DoesNotContain(forbiddenPropertyName, responsePropertyNames);
        }
    }
}
