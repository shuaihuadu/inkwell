// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.Tools;

/// <summary>
/// 验证 Agent 工具绑定到 MEAI 可执行函数的转换行为。
/// </summary>
[TestClass]
public sealed class AgentToolBindingResolverTests
{
    /// <summary>
    /// 验证解析结果保留目录元数据，并由绑定参数覆盖模型同名参数。
    /// </summary>
    [TestMethod]
    public async Task ResolveAsync_WithBoundParameters_ReturnsExecutableFunctionAsync()
    {
        // Arrange
        Guid toolId = Guid.CreateVersion7();
        AgentToolDefinition tool = new()
        {
            Id = toolId,
            Name = "get_weather",
            Description = "Gets the current weather.",
            ParametersJsonSchema = """
                {"type":"object","properties":{"city":{"type":"string"},"unit":{"type":"string"}}}
                """,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow,
        };
        string? invokedArguments = null;
        Dictionary<Guid, Func<string, CancellationToken, Task<string>>> executors = new()
        {
            [toolId] = (arguments, _) =>
            {
                invokedArguments = arguments;
                return Task.FromResult("{\"forecast\":\"sunny\"}");
            },
        };
        AgentToolBindingResolver resolver = new(
            new StubPersistenceProvider(new StubAgentToolRepository(tool)),
            executors);

        // Act
        IReadOnlyList<AIFunction> functions = await resolver.ResolveAsync(
            [new AgentToolBinding(toolId, "{\"unit\":\"fahrenheit\"}")]);
        object? result = await functions[0].InvokeAsync(new AIFunctionArguments
        {
            ["city"] = "Seattle",
            ["unit"] = "celsius",
        });

        // Assert
        Assert.HasCount(1, functions);
        Assert.AreEqual(tool.Name, functions[0].Name);
        Assert.AreEqual(tool.Description, functions[0].Description);
        Assert.AreEqual("object", functions[0].JsonSchema.GetProperty("type").GetString());
        JsonElement resultElement = (JsonElement)result!;
        Assert.AreEqual("sunny", resultElement.GetProperty("forecast").GetString());
        Assert.IsNotNull(invokedArguments);

        using JsonDocument argumentsDocument = JsonDocument.Parse(invokedArguments);
        Assert.AreEqual("Seattle", argumentsDocument.RootElement.GetProperty("city").GetString());
        Assert.AreEqual("fahrenheit", argumentsDocument.RootElement.GetProperty("unit").GetString());
    }

    private sealed class StubAgentToolRepository(AgentToolDefinition tool) : IAgentToolRepository
    {
        public Task<AgentToolDefinition> AddTool(AgentToolDefinition toolToAdd, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<AgentToolDefinition> GetTool(Guid id, CancellationToken ct = default) =>
            Task.FromResult(id == tool.Id ? tool : throw new KeyNotFoundException());

        public Task<AgentToolDefinition> GetToolByName(string name, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<PagedResult<AgentToolDefinition>> ListTools(Pagination pagination, SortOrder sort, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubPersistenceProvider(IAgentToolRepository tools) : IPersistenceProvider
    {
        public TRepository GetRepository<TRepository>() where TRepository : notnull =>
            tools is TRepository repository ? repository : throw new NotSupportedException();

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