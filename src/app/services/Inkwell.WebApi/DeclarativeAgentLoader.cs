using Inkwell.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using ChatOptions = Microsoft.Extensions.AI.ChatOptions;

namespace Inkwell.WebApi;

/// <summary>
/// 声明式 Agent 加载器
/// 从 YAML 文件加载 Agent 定义并注册到 AgentRegistry
/// </summary>
public static class DeclarativeAgentLoader
{
    /// <summary>
    /// 从指定目录加载所有 YAML Agent 定义
    /// </summary>
    /// <param name="registry">Agent 注册表</param>
    /// <param name="chatClient">LLM 客户端</param>
    /// <param name="agentsDirectory">YAML 文件目录路径</param>
    /// <param name="logger">日志记录器</param>
    /// <returns>加载的 Agent 数量</returns>
    public static int LoadFromDirectory(AgentRegistry registry, IChatClient chatClient, string agentsDirectory, ILogger? logger = null)
    {
        if (!Directory.Exists(agentsDirectory))
        {
            logger?.LogWarning("[DeclarativeAgent] Directory not found: {Directory}", agentsDirectory);
            return 0;
        }

        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();

        int count = 0;
        foreach (string filePath in Directory.EnumerateFiles(agentsDirectory, "*.yaml"))
        {
            try
            {
                string yaml = File.ReadAllText(filePath);
                AgentDefinition? definition = deserializer.Deserialize<AgentDefinition>(yaml);

                // [C5 修复] null check
                if (definition is null || string.IsNullOrWhiteSpace(definition.Name) || string.IsNullOrWhiteSpace(definition.Instructions))
                {
                    logger?.LogWarning("[DeclarativeAgent] Skipped invalid YAML: {File}", Path.GetFileName(filePath));
                    continue;
                }

                AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
                {
                    Name = definition.Name,
                    ChatOptions = new ChatOptions { Instructions = definition.Instructions },
                    ChatHistoryProvider = new InMemoryChatHistoryProvider(new()
                    {
                        ChatReducer = new MessageCountingChatReducer(10)
                    })
                });

                registry.Register(new AgentRegistration
                {
                    Id = definition.Name,
                    Name = definition.Description ?? definition.Name,
                    Description = $"[声明式] {definition.Description ?? definition.Name}",
                    Agent = agent,
                    AguiRoute = $"/api/agui/{definition.Name}"
                });

                logger?.LogInformation("[DeclarativeAgent] Registered: {AgentId}", definition.Name);
                count++;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[DeclarativeAgent] Failed to load: {File}", Path.GetFileName(filePath));
            }
        }

        return count;
    }

    /// <summary>
    /// YAML Agent 定义模型
    /// </summary>
    private sealed class AgentDefinition
    {
        /// <summary>
        /// 获取或设置 Agent 名称（同时作为 ID）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置 Agent 指令
        /// </summary>
        public string Instructions { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置 Agent 描述
        /// </summary>
        public string? Description { get; set; }
    }
}
