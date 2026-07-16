// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.AgentRuntime;

/// <summary>
/// 验证 Agent 编辑请求绑定到完整构建选项的解析行为。
/// </summary>
[TestClass]
public sealed class AgentBuildOptionsResolverTests
{
    /// <summary>
    /// 验证解析结果包含模型、聊天历史、Skill 定义与工具绑定参数。
    /// </summary>
    [TestMethod]
    public async Task ResolveAsync_WithToolAndSkills_ReturnsAvailableDefinitionsAsync()
    {
        // Arrange
        AgentToolBinding toolBinding = new(Guid.CreateVersion7(), "{\"city\":\"Shanghai\"}");
        AgentSkillDefinition skill = CreateSkillDefinition();
        AgentChatHistoryOptions historyOptions = new() { MaxMessagesToRetrieve = 20 };
        AgentModelOptions modelOptions = new() { ModelId = "test-model" };
        AgentUpsertRequest request = new()
        {
            Name = "Research assistant",
            ModelOptions = modelOptions,
            ToolBindings = [toolBinding],
            SkillBindings = [new AgentSkillBinding(skill.Id)],
            ChatHistoryOptions = historyOptions,
        };
        AgentBuildOptionsResolver resolver = new(new StubPersistenceProvider(new StubAgentSkillRepository(skill)));

        // Act
        AgentBuildOptions result = await resolver.ResolveAsync(request);

        // Assert
        Assert.AreSame(modelOptions, result.ModelOptions);
        Assert.AreSame(historyOptions, result.ChatHistoryOptions);
        Assert.HasCount(1, result.ToolBindings);
        Assert.AreSame(toolBinding, result.ToolBindings[0]);
        Assert.HasCount(1, result.Skills);
        Assert.AreSame(skill, result.Skills[0]);
    }

    private static AgentSkillDefinition CreateSkillDefinition() => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "source-review",
        Description = "Reviews sources.",
        Content = "Review every source before citing it.",
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };

    private sealed class StubAgentSkillRepository(AgentSkillDefinition skill) : IAgentSkillRepository
    {
        public Task<AgentSkillDefinition> AddSkill(AgentSkillDefinition skillToAdd, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<AgentSkillDefinition> GetSkill(Guid id, CancellationToken ct = default) =>
            Task.FromResult(id == skill.Id ? skill : throw new KeyNotFoundException());

        public Task<PagedResult<AgentSkillDefinition>> ListSkills(Pagination pagination, SortOrder sort, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubPersistenceProvider(IAgentSkillRepository skills) : IPersistenceProvider
    {
        public TRepository GetRepository<TRepository>() where TRepository : notnull =>
            skills is TRepository skillRepository
                ? skillRepository
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