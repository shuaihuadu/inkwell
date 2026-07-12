// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Protocols;

namespace Inkwell.WebApi.Tests.Protocols;

/// <summary>
/// 验证 MAF 官方协议端点的路由注册。
/// </summary>
[TestClass]
public sealed class AgentProtocolEndpointRouteBuilderExtensionsTests
{
    /// <summary>
    /// 验证 AG-UI 与三套 OpenAI API 的公开路径均已注册。
    /// </summary>
    [TestMethod]
    public void MapAgentProtocols_RegistersFourProtocolSurfaces()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddLogging();
        services.AddAgentProtocols();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        TestEndpointRouteBuilder endpoints = new(serviceProvider);

        // Act
        endpoints.MapAgentProtocols();
        string[] routePatterns =
        [
            .. endpoints.DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .Select(endpoint => endpoint.RoutePattern.RawText)
                .Where(pattern => pattern is not null)
                .Cast<string>(),
        ];

        // Assert
        CollectionAssert.Contains(routePatterns, "/agent/{agentId}");
        CollectionAssert.Contains(routePatterns, "/agent/{agentId}/v1/chat/completions/");
        CollectionAssert.Contains(routePatterns, "/agent/{agentId}/v1/responses/");
        CollectionAssert.Contains(routePatterns, "/v1/conversations/");
    }

    private sealed class TestEndpointRouteBuilder(IServiceProvider serviceProvider) : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(this.ServiceProvider);
    }
}