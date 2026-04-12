# Inkwell 需求文档

## 项目定位

Inkwell 是一个 AI 驱动的内容生产与智能体管理平台，基于 Microsoft Agent Framework（MAF）构建。项目的核心目标：

1. **覆盖 MAF 全部能力**：通过真实业务场景驱动，将 MAF 的 160+ 官方示例所涉及的能力全部落地
2. **端到端可运行**：从 Agent 对话到 Workflow 编排到文章发布，形成完整业务闭环
3. **生产级架构**：可插拔持久化、队列、文件存储，支持 DurableTask 持久化托管，集成 Aspire 可观测性

### MAF 能力覆盖矩阵

| 能力域          | 覆盖的 MAF 能力                                                                          | 对应需求章节 |
| --------------- | ---------------------------------------------------------------------------------------- | ------------ |
| Agent 基础      | `AIAgent`、`AsAIAgent()`、`ChatClientAgent`、Instructions、多轮对话、DI 集成              | 二           |
| Function Tools  | `AIFunctionFactory.Create()`、`ApprovalRequiredAIFunction`、`ToolApprovalRequestContent` | 2.3          |
| 结构化输出      | `ChatResponseFormat.ForJsonSchema<T>()`、`RunAsync<T>()`                                 | 2.4          |
| 记忆            | `ChatHistoryMemoryProvider`、`InMemoryVectorStore`、`PersistedConversationProvider`      | 2.5          |
| RAG             | `TextSearchProvider`、`TextSearchStore`、`TextSearchBehavior`                            | 2.6          |
| Skills          | `AgentSkillsProvider`、`SubprocessScriptRunner`、Class-based Skills                      | 2.7          |
| MCP             | `McpServerTool`、`AddMcpServer()`、`WithStdioServerTransport()`                          | 2.8          |
| 图像/多模态     | `DataContent.LoadFromAsync()`、`TextContent`                                             | 2.9          |
| 中间件          | `ChatClientAgent.Use()`、`FunctionCallMiddleware`、`GuardrailMiddleware`                 | 2.10         |
| 对话压缩        | `MessageCountingChatReducer`、`CompactionProvider`、`SlidingWindowCompactionStrategy`    | 2.11         |
| 对话持久化    | `PersistedConversationProvider`                                                          | 2.15         |
| 声明式 Agent    | `ChatClientPromptAgentFactory`、`CreateFromYamlAsync()`                                  | 2.12         |
| 后台响应        | `AllowBackgroundResponses`、`ContinuationToken`                                          | 2.13         |
| Agent作工具    | `AIFunctionFactory.Create()` 包装 Agent                                                  | 2.14         |
| 工具循环检查点  | Function Loop Checkpointing                                                              | 2.16         |
| Workflow 编排   | `WorkflowBuilder`、`AddEdge`、`AddFanOutEdge`、`AddFanInBarrierEdge`、`AddSwitch`、`AddMultiSelection` | 三  |
| 流式 Workflow   | `StreamingRun.WatchStreamAsync()`、`WorkflowEvent`                                       | 3.5          |
| 子工作流        | `ExecutorBinding.BindAsExecutor()`、嵌套 Workflow                                        | 3.6          |
| Workflow→Agent  | `Workflow.AsAIAgent()`、`IResettableExecutor`                                            | 3.7          |
| MapReduce       | 动态 Fan-Out + 聚合 Executor                                                             | 3.4          |
| 条件多选        | `AddMultiSelection`                                                                      | 3.4          |
| Checkpoint      | `CheckpointManager`、`RestoreCheckpointAsync()`、`SuperStepCompletedEvent`               | 3.9          |
| SharedState     | `IWorkflowContext.QueueStateUpdateAsync()`、`ReadStateAsync()`                           | 3.10         |
| GroupChat       | `GroupChatWorkflowBuilder`、`GroupChatManager`、`SelectNextAgentAsync`                   | 3.11         |
| Handoff         | `HandoffWorkflowBuilder`、`WithHandoff()`、`EnableReturnToPrevious()`                    | 3.12         |
| 声明式 Workflow | YAML 定义、`DeclarativeWorkflowBuilder`                                                  | 3.13         |
| 可视化          | `ToMermaidString()`、`ToDotString()`                                                     | 3.14         |
| DurableTask     | `ConfigureDurableAgents()`、`ConfigureDurableWorkflows()`、Azure Functions 托管          | 四           |
| A2A             | `A2ACardResolver`、`AgentCard.AsAIAgent()`                                               | 4.3          |
| AG-UI           | `MapAGUI`、SSE 协议、State Management                                                    | 五           |
| 可观测性        | `WithOpenTelemetry`、`WorkflowTelemetryOptions`、Aspire Dashboard                        | 1.6          |
| 授权            | ASP.NET Core Authorization、KeyCloak/JWT                                                 | 1.7          |

---

## 一、基础设施

### 1.1 LLM 配置

**需求**：通过 `appsettings.json` 配置 Azure OpenAI 连接信息，支持 user-secrets 覆盖。

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://xxx.openai.azure.com/",
    "ApiKey": "",
    "DeploymentName": "gpt-4o-mini"
  }
}
```

**验收标准**：
- `Endpoint` 和 `DeploymentName` 必填，缺失时启动报错
- `ApiKey` 为空时自动回退到 `AzureCliCredential`（开发环境）；有值时使用 `AzureKeyCredential`
- 通过 DI 注册为 `IChatClient` 单例
- 所有 Agent 和 Workflow 共享同一个 `IChatClient`

### 1.2 多模型服务

**需求**：不同场景可使用不同模型。内容写作用高质量模型（gpt-4o），分析/翻译等辅助任务用经济模型（gpt-4o-mini）。

```json
{
  "Models": {
    "Primary": { "DeploymentName": "gpt-4o" },
    "Secondary": { "DeploymentName": "gpt-4o-mini" },
    "Embedding": { "DeploymentName": "text-embedding-3-small" }
  }
}
```

**验收标准**：
- 按名称注册多个 `IChatClient`，通过 Keyed DI 或命名解析
- Writer Agent 使用 Primary，分析/翻译/SEO Agent 使用 Secondary
- Embedding 模型用于 Memory 和 RAG 场景

**MAF 能力**：MultiModelService（对应示例 `03-workflows/_StartHere/04_MultiModelService`）

### 1.3 持久化

**需求**：通过 Fluent API 切换持久化后端。

```csharp
builder.Services.AddInkwellCore()
    .UseInMemoryDatabase()          // 开发
    .UseSqlServer(connectionString) // 生产
```

**覆盖的实体**：文章（ArticleRecord）、运行记录（PipelineRunRecord）、分析报告（AnalysisRecord）、审核记录（ReviewRecord）

### 1.4 队列

**需求**：FIFO 队列 + 发布/订阅，支持 InMemory 和 Redis。

```csharp
builder.Services.AddInkwellCore()
    .UseInMemoryQueue()         // 开发
    .UseRedisQueue(redisConn)   // 生产
```

### 1.5 文件存储

**需求**：本地文件系统存储（默认），后续可扩展 Azure Blob Storage。

```csharp
builder.Services.AddInkwellCore()
    .UseLocalFileStorage("storage")
```

### 1.6 可观测性（Aspire 集成）

**需求**：集成 .NET Aspire，提供 OpenTelemetry Trace/Metrics/Logs 的统一管理。

**验收标准**：
- WebApi 项目集成 Aspire ServiceDefaults
- Workflow 执行通过 `WithOpenTelemetry` 注入 Trace
- 每个 Agent 的调用、Executor 的执行、SuperStep 的推进都有对应的 Span
- Aspire Dashboard（`http://localhost:18888`）可查看完整调用链
- 支持 `EnableSensitiveData` 配置（开发环境开启，生产环境关闭）
- 可选集成 Application Insights（生产环境）

**MAF 能力**：`WithOpenTelemetry`、`WorkflowTelemetryOptions`

### 1.7 授权与认证

**需求**：API 端点支持基于角色的访问控制。

**角色**：
- `admin`：全部权限
- `editor`：Agent 对话、Workflow 触发、文章管理
- `reviewer`：仅人工审核操作

**验收标准**：
- 集成 ASP.NET Core Authorization（JWT Bearer）
- AG-UI 端点和 REST API 均需认证
- 审核接口限制为 `reviewer` 或 `admin` 角色
- 开发环境可通过配置禁用认证

**MAF 能力**：Authorization middleware（对应示例 `05-end-to-end/AspNetAgentAuthorization`）

---

## 二、AIAgent

### 2.1 预定义 Agent

**需求**：系统内置多个专业 Agent，每个独立可用，也可作为 Workflow 节点。

| Agent ID              | 名称     | 职责                        | 模型      |
| --------------------- | -------- | --------------------------- | --------- |
| `writer`              | 内容写手 | 撰写高质量文章              | Primary   |
| `critic`              | 内容审核 | 审核文章质量，给出改进建议  | Primary   |
| `market-analyst`      | 市场分析 | 分析市场趋势和目标受众      | Secondary |
| `competitor-analyst`  | 竞品分析 | 分析竞品内容策略            | Secondary |
| `seo-optimizer`       | SEO 优化 | 分析和优化搜索引擎排名      | Secondary |
| `translator-english`  | 英文翻译 | 翻译为英文                  | Secondary |
| `translator-japanese` | 日文翻译 | 翻译为日文                  | Secondary |
| `image-analyst`       | 图片分析 | 分析图片内容、生成描述和ALT | Primary   |
| `coordinator`         | 智能调度 | 接待用户、路由到对应 Agent  | Secondary |

**验收标准**：
- 每个 Agent 通过 AG-UI 协议（SSE）独立对外暴露
- 前端可选择 Agent 进行对话
- Agent 注册表（`AgentRegistry`）提供元数据查询 API
- 每个 Agent 的 Instructions 使用中文
- Agent 通过 DI 注入依赖（`IChatClient`、`ILogger` 等），支持构造函数注入
- 运行时可通过 `MessageAIContextProvider` 动态注入额外上下文（如当前编辑中的文章信息）

**MAF 能力**：`AIAgent`、`ChatClientAgent`、DI 集成、`MessageAIContextProvider`（Additional Context）

### 2.2 Agent 注册与管理

**需求**：`AgentRegistry` 作为 Agent 的集中管理入口。

**API**：
- `GET /api/agents` — 获取所有 Agent 列表
- `GET /api/agents/{id}` — 获取 Agent 详情

### 2.3 Function Tools（函数工具）

**需求**：Agent 可调用外部函数作为工具，支持工具审批机制。

**业务场景**：
- Writer Agent 调用「搜索最新资讯」工具获取实时信息
- SEO Agent 调用「关键词分析」工具查询搜索量
- 调用敏感工具（如发布文章）时需要人工审批

**验收标准**：
- 使用 `AIFunctionFactory.Create()` 注册工具函数
- 工具返回结果自动注入 Agent 上下文
- 敏感工具标记为 `ApprovalRequiredAIFunction`，触发 `ToolApprovalRequestContent`

**MAF 能力**：`AIFunctionFactory`、`ApprovalRequiredAIFunction`、`ToolApprovalRequestContent`

### 2.4 结构化输出

**需求**：部分 Agent 返回结构化 JSON 而非自由文本。

**业务场景**：
- 市场分析 Agent 返回 `TopicAnalysis` 对象（热度评分、目标受众、关键词列表）
- SEO Agent 返回 `SeoReport` 对象（标题建议、关键词密度、元描述）

**验收标准**：
- 使用 `ChatResponseFormat.ForJsonSchema<T>()` 约束输出格式
- 使用 `RunAsync<T>()` 泛型方法直接反序列化

**MAF 能力**：`StructuredOutput`、`ChatResponseFormat.ForJsonSchema<T>()`

### 2.5 记忆与对话历史

**需求**：Agent 具备长期记忆能力，记住用户偏好和历史对话。

**业务场景**：
- Writer Agent 记住用户的写作风格偏好（正式/口语、长度偏好等）
- 对话历史持久化，重启后可恢复
- 长对话自动检索相关历史片段补充上下文

**验收标准**：
- 集成 `ChatHistoryMemoryProvider` + `InMemoryVectorStore`
- 配置 `StorageScope`（存储哪些消息）和 `SearchScope`（检索哪些消息）
- 使用 Embedding 模型生成向量
- 提供 `InMemoryChatHistoryProvider` 作为轻量替代（无向量检索）
- 对话历史持久化到外部存储（详见 2.15）

**MAF 能力**：`ChatHistoryMemoryProvider`、`InMemoryVectorStore`、`EmbeddingGenerator`、`StorageScope`、`SearchScope`

### 2.6 RAG（检索增强生成）

**需求**：Agent 可基于知识库检索相关文档，增强生成质量。

**业务场景**：
- Writer Agent 检索品牌风格指南、历史优质文章作为写作参考
- SEO Agent 检索行业关键词库和竞品分析报告

**验收标准**：
- 使用 `TextSearchProvider` + `TextSearchStore` 构建知识库
- 配置 `TextSearchBehavior.BeforeAIInvoke`（调用 LLM 前自动检索）
- 支持手动导入文档到知识库
- 检索结果与 Agent 的 AIContextProviders 集成

**MAF 能力**：`TextSearchProvider`、`TextSearchStore`、`TextSearchProviderOptions`、`TextSearchBehavior`

### 2.7 Agent Skills（技能）

**需求**：Agent 可执行预定义技能（脚本或代码），扩展能力边界。

**业务场景**：
- 文章 Markdown 格式校验与自动修复（lint 脚本）
- 可读性评分（Flesch-Kincaid 等算法，脚本计算）
- 敏感词扫描脚本（检测文章中的违禁/敏感关键词）
- SEO 元数据提取脚本（从 HTML 提取 title/description/keywords）
- 文章字数统计与目录自动生成

**验收标准**：
- 使用 `AgentSkillsProvider` 注册技能
- 支持 class-based Skills（C# 类定义）
- Skills 通过 DI 注入依赖
- 技能执行结果自动注入 Agent 上下文

**MAF 能力**：`AgentSkillsProvider`、Class-based Skills、DI 集成

### 2.8 MCP 集成（Model Context Protocol）

**需求**：Agent 通过 MCP 协议连接外部工具服务器。

**业务场景**：
- 连接本地 MCP 服务器查询 CMS 数据（文章列表、发布状态）
- 连接数据库 MCP 服务器查询分析数据

**验收标准**：
- 使用 `McpServerTool` 和 `AddMcpServer()` 注册 MCP 工具
- 支持 `WithStdioServerTransport()` 本地进程通信
- MCP 工具自动暴露为 Agent 可调用的 Function Tools

**MAF 能力**：`McpServerTool`、`AddMcpServer()`、`WithStdioServerTransport()`

### 2.9 图像/多模态处理

**需求**：Agent 可处理图片输入，支持视觉理解。

**业务场景**：
- Image Analyst Agent 分析上传图片，生成描述文字和 ALT 标签
- Writer Agent 根据图片内容生成配图说明

**验收标准**：
- 使用 `DataContent.LoadFromAsync()` 加载图片
- 图片与文本混合输入发送给多模态模型
- 支持常见图片格式（PNG、JPEG、WebP）

**MAF 能力**：`DataContent`、`TextContent`、多模态消息构造

### 2.10 Agent 中间件

**需求**：Agent 调用链支持中间件管线，实现横切关注点。

**业务场景**：
- 内容安全护栏：检测生成内容是否含敏感/违规信息
- 调用日志：记录每次 Agent 调用的输入输出
- 函数调用拦截：在工具调用前后执行自定义逻辑

**验收标准**：
- 使用 `ChatClientAgent.Use()` 注册中间件
- 实现 `GuardrailMiddleware`（内容安全检查）
- 实现 `FunctionCallMiddleware`（工具调用审计）
- 中间件按注册顺序执行

**MAF 能力**：`ChatClientAgent.Use()`、`FunctionCallMiddleware`、`GuardrailMiddleware`

### 2.11 对话压缩与缩减

**需求**：长对话自动压缩/缩减，防止超出模型上下文窗口。

**业务场景**：
- 长时间的写作对话（数十轮修改）自动压缩早期内容
- 工具调用结果较大时自动精简

**验收标准**：
- Chat Reduction：使用 `MessageCountingChatReducer` 按消息数触发缩减
- Compaction Pipeline：使用 `CompactionProvider` 配置多策略管线
  - `SlidingWindowCompactionStrategy`（滑动窗口）
  - `TruncationCompactionStrategy`（截断）
  - `SummarizationCompactionStrategy`（摘要）
  - `ToolResultCompactionStrategy`（工具结果压缩）
- 压缩触发阈值可配置

**MAF 能力**：`IChatReducer`、`MessageCountingChatReducer`、`CompactionProvider`、`PipelineCompactionStrategy`

### 2.12 声明式 Agent

**需求**：支持通过 YAML 文件定义 Agent，无需编写 C# 代码。

**业务场景**：
- 运营人员通过编辑 YAML 快速创建专题 Agent（如「春节营销 Agent」）
- YAML Agent 与代码 Agent 共存于 AgentRegistry

**验收标准**：
- 使用 `ChatClientPromptAgentFactory.CreateFromYamlAsync()` 加载
- YAML 定义 Agent 的 name、instructions、tools、options
- 声明式 Agent 自动注册到 AgentRegistry 并暴露 AG-UI 端点
- YAML 文件热加载（修改后重启生效）

**MAF 能力**：`ChatClientPromptAgentFactory`、`CreateFromYamlAsync()`

### 2.13 后台响应

**需求**：耗时较长的 Agent 任务支持后台执行，客户端异步轮询结果。

**业务场景**：
- 深度研究报告生成（单 Agent 多轮搜索 + 资料整理，耗时数分钟）
- 年度内容复盘报告（需汇总全年文章数据和分析趋势）

**验收标准**：
- 使用 `AgentRunOptions.AllowBackgroundResponses` 启用
- 返回 `ContinuationToken`，客户端可通过 token 查询进度
- 后台任务完成后通过回调/轮询获取结果
- 前端展示任务进度状态（排队中 → 处理中 → 完成）

**MAF 能力**：`AllowBackgroundResponses`、`ContinuationToken`、`AgentResponse`

### 2.14 Agent 作为函数工具（进程内）

**需求**：一个 Agent 可以把另一个 Agent 作为函数工具调用（进程内调用，区别于 4.3 A2A 跨进程通信）。

**业务场景**：
- Writer Agent 在写作过程中调用 SEO Agent 检查关键词密度
- Coordinator Agent 通过函数调用分发任务到专业 Agent

**验收标准**：
- 将 Agent 包装为 `AIFunctionFactory.Create()` 的函数
- 被调用 Agent 的完整对话能力保留
- 调用链在 Telemetry 中完整可追踪
- 与 A2A（4.3）的区别：本需求是进程内直接调用，A2A 是跨进程网络通信

**MAF 能力**：Agent-as-Function-Tool 模式

### 2.15 对话历史持久化

**需求**：Agent 对话历史持久化到外部存储，支持跨会话恢复。

**业务场景**：
- 用户关闭浏览器后重新打开，可恢复之前的对话上下文
- Writer Agent 的长期写作对话历史保存到数据库，不因服务重启丢失

**验收标准**：
- 实现 `PersistedConversationProvider` 将对话存储到 SqlServer
- 配置对话最大保留条数，超出自动归档
- 与 2.11 对话压缩配合：压缩后的对话也被持久化

**MAF 能力**：`PersistedConversationProvider`（对应示例 `02-agents/Agents/Step03`、`Step04`）

### 2.16 函数工具循环检查点

**需求**：Agent 执行长链工具调用循环时，支持中间检查点，避免失败后从头重试。

**业务场景**：
- Writer Agent 连续调用多个搜索工具收集素材时，若第 5 次调用失败，可从第 4 次检查点恢复

**验收标准**：
- 工具调用循环中周期性保存检查点
- 恢复时从最近检查点继续执行
- 与 3.9 Checkpoint 的区别：本需求是 Agent 级别的工具循环断点，3.9 是 Workflow 级别

**MAF 能力**：Function Loop Checkpointing（对应示例 `02-agents/Agents/Step19`）

---

## 三、Workflow

### 3.1 内容生产流水线（串行 + 并行 + HITL）

**需求**：完整的内容生产流水线，组合串行、并行、条件路由、人工审核等编排模式。

**拓扑**：

```
输入(主题) → InputDispatch
              ├─ Fan-Out → MarketAnalysis ────┐
              └─ Fan-Out → CompetitorAnalysis ─┤ Fan-In Barrier
                                               ▼
                                    AnalysisAggregation
                                               │
                                    ┌── Writer ◀──┐
                                    │      │      │ Critic 退回
                                    │      ▼      │
                                    │   Critic ───┘
                                    │      │ 通过
                                    │      ▼
                                    │  RequestPort(人工审核) ⏸
                                    │      │
                                    │      ▼
                                    │  ReviewGate → [发布] / [退回 → Writer]
```

**MAF 能力覆盖**：
- `AddFanOutEdge` / `AddFanInBarrierEdge`（并行）
- `AddEdge`（串行）
- `AddSwitch`（条件路由）
- `RequestPort`（Human-in-the-Loop）
- `SharedState`（共享状态）
- `YieldOutputAsync`（多阶段输出）

### 3.2 多语言翻译流水线（Fan-Out / Fan-In）

**需求**：一篇文章同时翻译为多种语言。

```
输入(文章) → Fan-Out → TranslatorEN ──┐
                     → TranslatorJA ──┤ Fan-In Barrier
                     → TranslatorFR ──┘
                            ▼
                     TranslationAggregator → [输出: 多语言版本]
```

**MAF 能力**：Fan-Out / Fan-In、Agent 绑定（`BindAsExecutor`）

### 3.3 Writer-Critic 循环（Loop）

**需求**：写作 → 审核 → 修改的迭代循环，直到审核通过或达到最大轮次。

```
Writer → Critic ─┬─ 不通过 → Writer（循环）
                 └─ 通过 → 输出
```

**验收标准**：
- 使用 `AddEdge` 构建反向边实现循环
- 最大循环次数可配置（默认 3 次）
- 每轮 Critic 的反馈作为 Writer 下一轮的输入

**MAF 能力**：Loop 模式（`AddEdge` 反向边）

### 3.4 批量内容评估（MapReduce）

**需求**：对 N 篇待发布文章同时进行质量评估，每篇文章独立评分，最终汇总排序。

```
输入(N篇文章) → Dispatcher → 动态 Fan-Out ─┬─ Evaluator(文章1) ──┐
                                           ├─ Evaluator(文章2) ──┤
                                           ├─ ...              ──┤ Fan-In Barrier
                                           └─ Evaluator(文章N) ──┘
                                                                  ▼
                                                     RankAggregator → [排序评估报告]
```

**验收标准**：
- Dispatcher 根据输入文章数量动态生成 Fan-Out 任务
- 每个 Evaluator 对单篇文章进行多维评分（可读性、SEO、原创性）
- RankAggregator 汇总所有评分并按综合得分排序
- 支持条件多选（`AddMultiSelection`）：评分低于阈值的文章同时路由到「退回修改」和「通知编辑」两条边

**MAF 能力**：MapReduce 模式（`AddFanOutEdge` + `AddFanInBarrierEdge`）、`AddMultiSelection`（条件多选路由）

### 3.5 流式 Workflow 输出

**需求**：Workflow 执行过程中实时推送事件流到客户端。

**验收标准**：
- 使用 `StreamingRun.WatchStreamAsync()` 监听事件流
- 前端通过 SSE 接收 `WorkflowEvent`（`ExecutorCompletedEvent`、`WorkflowOutputEvent`、`SuperStepCompletedEvent`）
- 每个 Executor 完成时实时通知前端更新 UI

**MAF 能力**：`StreamingRun`、`WatchStreamAsync()`、`WorkflowEvent`

### 3.6 子工作流（Sub-Workflows）

**需求**：主工作流可嵌套调用子工作流，实现模块化编排。

**业务场景**：
- 内容生产流水线中，「翻译」步骤调用_翻译子工作流_（3.2）
- 主流水线无需关心翻译的内部并行逻辑

**验收标准**：
- 使用 `ExecutorBinding.BindAsExecutor()` 将子 Workflow 绑定为父 Workflow 的 Executor
- 子工作流的输入/输出类型与父 Executor 接口匹配
- 子工作流的 Telemetry Span 嵌套在父 Span 下

**MAF 能力**：`ExecutorBinding.BindAsExecutor()`、SubworkflowBinding

### 3.7 Workflow 作为 Agent

**需求**：将 Workflow 包装为 Agent，用户可通过对话交互驱动 Workflow。

**业务场景**：
- 用户在聊天界面输入主题，实际触发内容生产流水线
- Workflow 的多阶段输出作为对话消息逐条回复

**验收标准**：
- 使用 `Workflow.AsAIAgent()` 转换
- 转换后的 Agent 支持 `CreateSessionAsync()` 和 `RunStreamingAsync()`
- 注册到 AgentRegistry，暴露 AG-UI 端点
- Workflow 的 `YieldOutputAsync` 输出映射为 Agent 响应消息

**MAF 能力**：`Workflow.AsAIAgent()`、`IResettableExecutor`

### 3.8 混合 Workflow（Agent + Executor 混编）

**需求**：同一 Workflow 中混合使用 AI Agent 和普通 Executor。

> 注：3.1 内容生产流水线本身就是混合编排的典型实现（AnalysisAggregation 是纯代码 Executor，Writer/Critic 是 AI Agent）。本需求确认该模式作为独立的 MAF 能力被显式覆盖。

**业务场景**：
- 纯代码 Executor（数据验证、格式转换、聚合计算）与 AI Agent（内容生成、审核）在同一流程中协作
- InputDispatch / AnalysisAggregation / ReviewGate 均为确定性 Executor，Writer / Critic 为 AI Agent

**MAF 能力**：`MixedWorkflowAgentsAndExecutors` 模式（3.1 已实际覆盖）

### 3.9 Checkpoint 与 Resume

**需求**：长时间运行的 Workflow 支持检查点持久化和恢复。

**业务场景**：
- 内容流水线在人工审核阶段暂停，服务重启后可从检查点恢复
- 流水线失败后从最近的检查点重试，而非从头开始

**验收标准**：
- 使用 `CheckpointManager` 定义检查点策略
- 每个 `SuperStepCompletedEvent` 触发自动保存
- 使用 `RestoreCheckpointAsync()` 从持久化状态恢复
- Checkpoint 数据存储在配置的持久化后端

**MAF 能力**：`CheckpointManager`、`CheckpointInfo`、`RestoreCheckpointAsync()`、`SuperStepCompletedEvent`

### 3.10 共享状态（Shared States）

**需求**：Workflow 中的多个 Executor 可通过共享状态传递跨节点数据。

**业务场景**：
- 分析阶段的结果写入共享状态，写作阶段读取
- 审核反馈写入共享状态，Writer 下一轮读取

**验收标准**：
- 使用 `IWorkflowContext.QueueStateUpdateAsync()` 写入状态
- 使用 `ReadStateAsync()` 读取状态
- 状态更新在 SuperStep 边界生效

**MAF 能力**：`QueueStateUpdateAsync()`、`ReadStateAsync()`

### 3.11 选题讨论会（GroupChat）

**需求**：多个角色 Agent 围绕选题轮流发言讨论，由 Manager 控制发言顺序和终止条件。

**参与者**：
- 市场分析师：从市场角度评估选题
- 内容编辑：从内容质量角度评估
- SEO 专家：从搜索优化角度评估
- 主持人（Manager）：控制讨论节奏，判断是否达成共识

**验收标准**：
- 使用 `AgentWorkflowBuilder.CreateGroupChatBuilderWith` 构建
- `GroupChatManager` 实现自定义的发言选择逻辑
- 最大讨论轮次可配置（默认 10 轮）
- 讨论结果作为选题的最终决策输出
- 支持 GroupChat 中的工具审批（`GroupChatToolApproval`）

**MAF 能力**：`GroupChatWorkflowBuilder`、`GroupChatManager`、`SelectNextAgentAsync`

### 3.12 智能路由（Handoff）

**需求**：用户提问后根据问题类型自动切换到对应专业 Agent，处理完后可返回。

**角色**：
- Coordinator Agent：初始接待，判断问题类型
- Writer Agent：处理写作相关问题
- SEO Agent：处理 SEO 相关问题
- Translator Agent：处理翻译相关问题

**验收标准**：
- 使用 `AgentWorkflowBuilder.CreateHandoffBuilderWith` 构建
- `WithHandoff(from, to, reason)` 定义切换关系
- `EnableReturnToPrevious()` 允许返回上一个 Agent
- 切换过程对用户透明

**MAF 能力**：`HandoffWorkflowBuilder`（`[Experimental]`）

### 3.13 声明式 Workflow

**需求**：通过 YAML 文件定义 Workflow，降低编排门槛。

**业务场景**：
- 运营人员通过 YAML 定义简单的内容审核流程
- YAML 定义与代码定义的 Workflow 共存
- 示例：**快速内容审批流**（4 节点串行）

```yaml
# quick-review.yaml
name: QuickContentReview
executors:
  - id: validator
    type: InputValidator
  - id: reviewer
    type: ContentReviewer
  - id: gate
    type: ApprovalGate
  - id: publisher
    type: ArticlePublisher
edges:
  - from: validator
    to: reviewer
  - from: reviewer
    to: gate
  - from: gate
    to: publisher
```

**验收标准**：
- YAML 定义 Executor 列表、边、条件路由
- 支持在 YAML 中引用已注册的 Agent 和工具
- 加载后与代码构建的 Workflow 行为一致

**MAF 能力**：Declarative Workflow（对应示例 `03-workflows/Declarative/`）

### 3.14 Workflow 可视化

**需求**：所有 Workflow 支持导出拓扑图。

**验收标准**：
- `ToMermaidString()` 导出 Mermaid 格式
- `ToDotString()` 导出 Graphviz DOT 格式
- 前端页面展示 Workflow 拓扑（使用 React Flow 或 Mermaid 渲染）

**MAF 能力**：`Visualization`

---

## 四、Hosting（托管）

### 4.1 DurableTask Console 托管

**需求**：Agent 和 Workflow 通过 DurableTask 框架实现持久化运行，Console 应用托管。

**业务场景**：
- 内容生产流水线（3.1）的 DurableTask 版本：HITL 人工审核可能等待数小时甚至数天，普通 InProcess 执行无法在服务重启后恢复等待状态，DurableTask 将编排状态持久化到外部存储
- 后台批量文章评估（3.4）：大量文章的 MapReduce 评估任务通过 DurableTask 持久化，避免因进程崩溃导致全部重做

**验收标准**：
- Agent 使用 `ConfigureDurableAgents()` 注册
- Workflow 使用 `ConfigureDurableWorkflows()` 注册
- 任务状态持久化到配置的存储后端
- 支持编排模式：链式调用、并发、条件分支

**MAF 能力**：`ConfigureDurableAgents()`、`ConfigureDurableWorkflows()`

### 4.2 DurableTask Azure Functions 托管

**需求**：Agent 和 Workflow 托管在 Azure Functions 上，实现 Serverless 部署。

**验收标准**：
- 使用 `FunctionsApplication` 配置 Durable Agent/Workflow
- HTTP Trigger 接收请求、Durable Orchestration 执行编排
- 支持 HITL（External Events）和 Long-running Tools

**MAF 能力**：Azure Functions DurableTask 集成

### 4.3 A2A（Agent-to-Agent）

**需求**：Agent 之间通过 A2A 协议跨进程通信，实现分布式 Agent 架构。

**业务场景**：
- Writer Agent 部署在服务 A，SEO Agent 部署在服务 B
- 服务 A 通过 A2A 协议远程调用服务 B 的 SEO Agent

**验收标准**：
- 使用 `A2ACardResolver` 发现远程 Agent
- 使用 `AgentCard.AsAIAgent()` 将远程 Agent 包装为本地可调用的 Agent
- 远程调用对业务代码透明
- 支持将远程 Agent 作为函数工具（`A2AAgent_AsFunctionTools`）

**MAF 能力**：`A2ACardResolver`、`AgentCard`、A2A 协议

---

## 五、前端

### 5.1 Dashboard

**需求**：系统总览页。

**展示内容**：
- Agent 数量 / Workflow 数量
- 流水线运行次数 / 完成次数
- 文章总数 / 已发布数
- 审核通过率
- 最近运行记录列表

### 5.2 Agent 对话

**需求**：选择任意 Agent 进行对话。

**UI 组件**：
- Ant Design X `Bubble` 组件渲染对话
- Ant Design X `Sender` 组件输入
- Agent 选择器（下拉框）
- 新对话按钮
- 支持上传图片（多模态场景）

**对接方式**：通过 AG-UI 协议（SSE）对接后端 Agent

**AG-UI 高级功能**：
- 后端工具调用：Agent 的 Function Tool 调用结果实时展示
- 前端工具调用：Agent 请求前端执行操作（如打开预览）
- HITL 状态同步：工具审批请求推送到前端确认
- 状态管理：前端 Agent 状态通过 AG-UI State 事件同步

### 5.3 Workflow 管理

**需求**：展示系统中的 Workflow 列表，可触发运行。

**功能**：
- Workflow 列表（名称、描述、节点数、状态）
- 运行 Workflow（输入参数 → SSE 实时事件流）
- 实时进度展示（每个 Executor 完成时高亮拓扑节点）
- 人工审核界面（HITL 场景）
- Workflow 拓扑图展示（Mermaid 渲染）

### 5.4 知识库管理

**需求**：管理 RAG 知识库中的文档。

**功能**：
- 文档列表（名称、类型、上传时间）
- 上传文档（TXT、Markdown）
- 删除文档
- 文档向量化状态展示

### 5.5 DevUI 调试界面

**需求**：开发环境下的 Agent 调试界面。

**功能**：
- Agent 的 Prompt 实时查看和编辑
- Function Tool 调用日志
- 对话历史查看（含系统消息）
- Token 使用统计

**MAF 能力**：DevUI（对应示例 `02-agents/DevUI`）

---

## 六、API 总览

### REST API

| Controller | Method | Route                              | 说明              |
| ---------- | ------ | ---------------------------------- | ----------------- |
| Health     | GET    | `/api/health`                      | 健康检查          |
| Dashboard  | GET    | `/api/dashboard/stats`             | Dashboard 统计    |
| Agents     | GET    | `/api/agents`                      | Agent 列表        |
| Agents     | GET    | `/api/agents/{id}`                 | Agent 详情        |
| Articles   | GET    | `/api/articles`                    | 文章列表          |
| Articles   | GET    | `/api/articles/{id}`               | 文章详情          |
| Articles   | GET    | `/api/articles/status/{status}`    | 按状态查询        |
| Pipeline   | GET    | `/api/pipeline/runs`               | 运行记录列表      |
| Pipeline   | GET    | `/api/pipeline/runs/{id}`          | 运行记录详情      |
| Pipeline   | POST   | `/api/pipeline/run`                | 启动流水线（SSE） |
| Reviews    | GET    | `/api/reviews/{articleId}`         | 审核记录          |
| Reviews    | POST   | `/api/reviews/{articleId}/approve` | 审核通过          |
| Reviews    | POST   | `/api/reviews/{articleId}/reject`  | 审核退回          |
| Analyses   | GET    | `/api/analyses/{pipelineRunId}`    | 分析报告          |
| Knowledge  | GET    | `/api/knowledge`                   | 知识库文档列表    |
| Knowledge  | POST   | `/api/knowledge/upload`            | 上传文档          |
| Knowledge  | DELETE | `/api/knowledge/{id}`              | 删除文档          |
| Workflows  | GET    | `/api/workflows`                   | Workflow 列表     |
| Workflows  | GET    | `/api/workflows/{id}`              | Workflow 详情     |
| Workflows  | GET    | `/api/workflows/{id}/topology`     | Workflow 拓扑图   |
| Workflows  | POST   | `/api/workflows/{id}/run`          | 运行 Workflow     |

### AG-UI 端点

通过 `MapAGUI` 自动注册，每个 Agent 一个端点：

```
POST /api/agui/{agent-id}
```

---

## 七、非功能性需求

### 安全
- 所有密钥通过 user-secrets 或环境变量管理
- `ApiKey` 不硬编码
- `EnableSensitiveData` 仅在开发环境启用
- 文件存储防路径遍历攻击
- JWT 认证 + 角色授权

### 代码规范
- 文件作用域命名空间
- 主构造函数（Primary Constructor）
- `this.` 前缀，显式类型（不用 `var`）
- XML 文档注释（中文 summary，英文异常消息）
- 提示词使用中文
- 一个类一个文件

### 前端规范
- kebab-case 文件命名
- TypeScript strict mode
- Ant Design 6 + Ant Design X

---

## 附录：MAF 示例覆盖清单

以下列出 MAF 官方示例与 Inkwell 需求的映射关系，确保全覆盖。

### 01-get-started（6 个示例）
| 示例               | 对应需求                 |
| ------------------ | ------------------------ |
| 01_hello_agent     | 2.1 预定义 Agent         |
| 02_add_tools       | 2.3 Function Tools       |
| 03_multi_turn      | 2.5 记忆与对话历史       |
| 04_memory          | 2.5 记忆与对话历史       |
| 05_first_workflow  | 3.1 内容生产流水线       |
| 06_host_your_agent | 4.2 Azure Functions 托管 |

### 02-agents（75+ 个示例）
| 能力域               | 对应需求                    |
| -------------------- | --------------------------- |
| Agents Step01-02     | 2.3 Function Tools          |
| Agents Step02        | 2.4 结构化输出              |
| Agents Step03-04     | 2.15 对话历史持久化         |
| Agents Step05        | 1.6 可观测性                |
| Agents Step06        | 2.1（DI 集成）              |
| Agents Step07        | 2.8 MCP 集成                |
| Agents Step08        | 2.9 图像/多模态             |
| Agents Step09        | 2.3 Function Tools          |
| Agents Step11        | 2.10 Agent 中间件           |
| Agents Step12        | 2.7 Agent Skills            |
| Agents Step13        | 2.12 声明式 Agent           |
| Agents Step14        | 2.13 后台响应               |
| Agents Step15        | 2.1（Additional Context）   |
| Agents Step16/18     | 2.11 对话压缩与缩减         |
| Agents Step19        | 2.16 函数工具循环检查点     |
| AgentWithMemory      | 2.5 记忆与对话历史          |
| AgentWithRAG         | 2.6 RAG                     |
| AgentSkills          | 2.7 Agent Skills            |
| ModelContextProtocol | 2.8 MCP 集成                |
| AGUI                 | 五（前端 AG-UI 集成）       |
| AgentOpenTelemetry   | 1.6 可观测性                |
| DeclarativeAgents    | 2.12 声明式 Agent           |
| DevUI                | 5.5 DevUI 调试界面          |
| AgentProviders       | 范围外（多提供商覆盖）      |

### 03-workflows（40+ 个示例）
| 能力域            | 对应需求                 |
| ----------------- | ------------------------ |
| _StartHere 01     | 3.5 流式 Workflow        |
| _StartHere 02-03  | 3.1 / 3.8 混合编排       |
| _StartHere 04     | 1.2 多模型服务           |
| _StartHere 05     | 3.6 子工作流             |
| _StartHere 06     | 3.8 混合 Workflow        |
| _StartHere 07     | 3.3 Writer-Critic 循环   |
| Concurrent        | 3.4 MapReduce            |
| ConditionalEdges  | 3.1（条件路由）+ 3.4（多选）|
| Loop              | 3.3 Writer-Critic 循环   |
| HumanInTheLoop    | 3.1（HITL）              |
| Checkpoint        | 3.9 Checkpoint 与 Resume |
| SharedStates      | 3.10 共享状态            |
| Visualization     | 3.14 可视化              |
| Observability     | 1.6 可观测性             |
| Declarative       | 3.13 声明式 Workflow     |
| GroupChat         | 3.11 GroupChat           |
| WorkflowAsAnAgent | 3.7 Workflow 作为 Agent  |

### 04-hosting（20+ 个示例）
| 能力域           | 对应需求                   |
| ---------------- | -------------------------- |
| DurableAgents    | 4.1 / 4.2 DurableTask 托管 |
| DurableWorkflows | 4.1 / 4.2 DurableTask 托管 |
| A2A              | 4.3 A2A                    |

### 05-end-to-end（21+ 个示例）
| 示例                       | 对应需求                     |
| -------------------------- | ---------------------------- |
| AgentWebChat / AGUIWebChat | 五（前端）                   |
| A2AClientServer            | 4.3 A2A                      |
| AGUIClientServer           | 五（AG-UI 集成）             |
| AspNetAgentAuthorization   | 1.7 授权与认证               |
| HostedAgents               | 4.1 / 4.2 托管 + 2.6 RAG     |
| M365Agent                  | 范围外（M365 产品集成）      |
| AgentWithPurview           | 范围外（Purview 产品集成）   |

### 范围外说明

以下 MAF 能力属于微软特定产品集成或多提供商适配，不属于 MAF 核心编程模型，Inkwell 不做覆盖：

| 能力                     | 原因                                                                 |
| ------------------------ | -------------------------------------------------------------------- |
| AgentProviders 多提供商  | Anthropic/Ollama/ONNX/Google Gemini 等 16 种提供商适配，Inkwell 聚焦 Azure OpenAI |
| M365Agent                | Microsoft 365 Teams/Copilot 集成，需 M365 订阅                      |
| AgentWithPurview         | Microsoft Purview 数据治理集成，需 Purview 服务                      |
| AgentsWithFoundry        | Azure AI Foundry 特有能力（Computer Use、Fabric 等），需 Foundry 订阅 |

### 可观测性
- 集成 Aspire ServiceDefaults
- OpenTelemetry Trace 覆盖 Agent 调用和 Workflow 执行
- Aspire Dashboard 开发环境默认启用
