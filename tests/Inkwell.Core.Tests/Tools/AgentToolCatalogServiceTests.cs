// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.Tools;

/// <summary>
/// 验证 Agent Tool 目录查询与绑定参数校验。
/// </summary>
[TestClass]
public sealed class AgentToolCatalogServiceTests
{
    /// <summary>
    /// 验证缺少工具必填参数时拒绝绑定。
    /// </summary>
    [TestMethod]
    public async Task ValidateToolBindingAsync_WithoutRequiredField_ThrowsArgumentExceptionAsync()
    {
        // Arrange
        AgentToolDefinition tool = CreateToolDefinition();
        AgentToolCatalogService service = new(new StubPersistenceProvider(new StubAgentToolRepository(tool)));

        // Act
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ValidateToolBindingAsync(tool.Id, "{}"));

        // Assert
        StringAssert.Contains(exception.Message, "missing required parameter: 'timeZoneId'");
    }

    private static AgentToolDefinition CreateToolDefinition() => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "get_current_datetime",
        Description = "Gets the current date and time.",
        ParametersJsonSchema = "{\"type\":\"object\",\"required\":[\"timeZoneId\"]}",
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private sealed class StubAgentToolRepository(AgentToolDefinition tool) : IAgentToolRepository
    {
        public Task<AgentToolDefinition> AddTool(AgentToolDefinition toolToAdd, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<AgentToolDefinition> GetTool(Guid id, CancellationToken ct = default) =>
            Task.FromResult(id == tool.Id ? tool : throw new KeyNotFoundException());

        public Task<AgentToolDefinition> GetToolByName(string name, CancellationToken ct = default) =>
            Task.FromResult(name == tool.Name ? tool : throw new KeyNotFoundException());

        public Task<PagedResult<AgentToolDefinition>> ListTools(Pagination pagination, SortOrder sort, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubPersistenceProvider(IAgentToolRepository tools) : IPersistenceProvider
    {
        public TRepository GetRepository<TRepository>() where TRepository : notnull =>
            tools is TRepository toolRepository
                ? toolRepository
                : throw new NotSupportedException();

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel, Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}