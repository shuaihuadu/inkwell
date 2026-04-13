using Inkwell.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Inkwell.WebApi;

/// <summary>
/// 声明式 Workflow 加载器
/// 从 YAML 文件加载 Workflow 定义并注册到 WorkflowRegistry
/// </summary>
public static class DeclarativeWorkflowLoader
{
    /// <summary>
    /// 从指定目录加载所有 YAML Workflow 定义
    /// </summary>
    /// <param name="registry">Workflow 注册表</param>
    /// <param name="chatClient">LLM 客户端</param>
    /// <param name="workflowsDirectory">YAML 文件目录路径</param>
    /// <param name="logger">日志记录器</param>
    /// <returns>加载的 Workflow 数量</returns>
    public static int LoadFromDirectory(WorkflowRegistry registry, IChatClient chatClient, string workflowsDirectory, ILogger? logger = null)
    {
        if (!Directory.Exists(workflowsDirectory))
        {
            logger?.LogWarning("[DeclarativeWorkflow] Directory not found: {Directory}", workflowsDirectory);
            return 0;
        }

        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();

        int count = 0;
        foreach (string filePath in Directory.EnumerateFiles(workflowsDirectory, "*.yaml"))
        {
            try
            {
                string yaml = File.ReadAllText(filePath);
                WorkflowDefinition? definition = deserializer.Deserialize<WorkflowDefinition>(yaml);

                // [C5 修复] null check
                if (definition is null || string.IsNullOrWhiteSpace(definition.Name) || definition.Executors.Count == 0)
                {
                    logger?.LogWarning("[DeclarativeWorkflow] Skipped invalid YAML: {File}", Path.GetFileName(filePath));
                    continue;
                }

                Workflow? workflow = BuildWorkflowFromDefinition(definition, chatClient);
                if (workflow is null)
                {
                    logger?.LogWarning("[DeclarativeWorkflow] Failed to build workflow: {Name}", definition.Name);
                    continue;
                }

                string workflowId = $"declarative-{definition.Name.ToLowerInvariant()}";
                registry.Register(new WorkflowRegistration
                {
                    Id = workflowId,
                    Name = $"[声明式] {definition.Name}",
                    Description = definition.Description ?? definition.Name,
                    Workflow = workflow
                });

                logger?.LogInformation("[DeclarativeWorkflow] Registered: {WorkflowId}", workflowId);
                count++;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[DeclarativeWorkflow] Failed to load: {File}", Path.GetFileName(filePath));
            }
        }

        return count;
    }

    /// <summary>
    /// 根据 YAML 定义构建 Workflow
    /// </summary>
    private static Workflow? BuildWorkflowFromDefinition(WorkflowDefinition definition, IChatClient chatClient)
    {
        if (definition.Executors.Count == 0 || definition.Edges.Count == 0)
        {
            return null;
        }

        // 为每个 Executor 创建基于 AI 的实例
        Dictionary<string, DeclarativeExecutor> executors = [];
        foreach (ExecutorDefinition execDef in definition.Executors)
        {
            DeclarativeExecutor executor = new(chatClient, execDef.Id, execDef.Type);
            executors[execDef.Id] = executor;
        }

        // 确定入口 Executor（第一个边的 from）
        string entryId = definition.Edges[0].From;
        if (!executors.TryGetValue(entryId, out DeclarativeExecutor? entryExecutor))
        {
            return null;
        }

        // 构建 Workflow
        WorkflowBuilder builder = new WorkflowBuilder(entryExecutor)
            .WithName(definition.Name)
            .WithDescription(definition.Description ?? definition.Name);

        foreach (EdgeDefinition edge in definition.Edges)
        {
            if (executors.TryGetValue(edge.From, out DeclarativeExecutor? from) &&
                executors.TryGetValue(edge.To, out DeclarativeExecutor? to))
            {
                builder.AddEdge(from, to);
            }
        }

        // 最后一个 Executor 作为输出
        string lastId = definition.Edges[^1].To;
        if (executors.TryGetValue(lastId, out DeclarativeExecutor? lastExecutor))
        {
            builder.WithOutputFrom(lastExecutor);
        }

        return builder.Build();
    }

    /// <summary>
    /// YAML Workflow 定义
    /// </summary>
    private sealed class WorkflowDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<ExecutorDefinition> Executors { get; set; } = [];
        public List<EdgeDefinition> Edges { get; set; } = [];
    }

    /// <summary>
    /// YAML Executor 定义
    /// </summary>
    private sealed class ExecutorDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// YAML Edge 定义
    /// </summary>
    private sealed class EdgeDefinition
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
    }
}

/// <summary>
/// 声明式 Executor：通用 AI 执行器，根据类型名称生成对应的 Prompt
/// </summary>
[SendsMessage(typeof(string))]
internal sealed class DeclarativeExecutor : Executor<string>
{
    private readonly AIAgent _agent;

    public DeclarativeExecutor(IChatClient chatClient, string id, string typeName) : base(id)
    {
        string instructions = typeName switch
        {
            "InputValidator" => "你是一名输入验证器。检查输入内容是否有效、完整，不含违规内容。如果有效则原样输出，否则输出错误信息。",
            "ContentReviewer" => "你是一名内容审核员。审核文章的质量、准确性和合规性。给出审核意见和建议。",
            "ApprovalGate" => "你是一名审批网关。根据上游审核意见决定是批准还是退回。输出'批准'或'退回'及原因。",
            "ArticlePublisher" => "你是一名发布执行器。确认文章已通过审核，输出发布确认信息。",
            _ => $"你是一名 {typeName} 处理器。处理上游传入的内容并输出结果。"
        };

        this._agent = chatClient.AsAIAgent(instructions: instructions + " 请用中文回复。");
    }

    /// <inheritdoc />
    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        AgentResponse response = await this._agent.RunAsync(message, cancellationToken: cancellationToken);
        await context.SendMessageAsync(response.Text, cancellationToken: cancellationToken);
        await context.YieldOutputAsync(response.Text, cancellationToken: cancellationToken);
    }
}
