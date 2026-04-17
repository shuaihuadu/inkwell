# Inkwell 架构设计文档

## 一、系统概述

Inkwell 是一个 AI 驱动的内容生产平台，基于 Microsoft Agent Framework（MAF）编排多个 AI Agent 和人工环节，实现从选题到发布的端到端自动化。

### 技术栈

| 层级       | 技术                                                                              |
| ---------- | --------------------------------------------------------------------------------- |
| 后端框架   | .NET 10 / ASP.NET Core                                                            |
| AI 编排    | Microsoft Agent Framework 1.1.0                                                   |
| LLM 客户端 | Microsoft.Extensions.AI + Microsoft Foundry                                       |
| 持久化     | Entity Framework Core 10（InMemory / SQL Server）                                 |
| 向量存储   | Microsoft.Extensions.VectorData + InMemory（可切换 Qdrant / Azure AI Search）     |
| 队列       | InMemory / Redis（StackExchange.Redis）                                           |
| 前端       | React 19 + TypeScript + Ant Design 6 + Ant Design X                               |
| 流程图     | Mermaid（Workflow 拓扑可视化）                                                    |
| 容器化     | Docker Compose（WebApi、Webapp、A2A、DTS Emulator、SQL Server、Aspire Dashboard） |

---

## 二、项目结构

```
Inkwell/
+-- src/
|   +-- core/                                        # 核心层
|   |   +-- Inkwell.Abstractions/                    # 接口定义 + 模型
|   |   |   +-- Models/                              # 数据模型（Record + 业务模型）
|   |   |   +-- Persistence/                         # 持久化接口
|   |   |   |   +-- ISessionPersistenceProvider.cs   # 会话持久化接口
|   |   |   |   +-- IArticlePersistenceProvider.cs
|   |   |   |   +-- IPipelineRunPersistenceProvider.cs
|   |   |   |   +-- IAnalysisPersistenceProvider.cs
|   |   |   |   +-- IReviewPersistenceProvider.cs
|   |   |   +-- Queue/                               # 队列接口
|   |   |   +-- Storage/                             # 文件存储接口
|   |   |   +-- IPersistenceProvider.cs              # 泛型 CRUD 接口
|   |   |   +-- InkwellCoreBuilder.cs                # 服务构建器
|   |   |
|   |   +-- Inkwell.Core/                            # Agent + Workflow 实现
|   |   |   +-- Agents/                              # Agent 定义与服务
|   |   |   |   +-- InkwellAgents.cs                 # 10+ Agent 定义
|   |   |   |   +-- AgentRegistry.cs                 # Agent 注册表
|   |   |   |   +-- AgentMemoryService.cs            # 长期记忆（向量存储）
|   |   |   |   +-- KnowledgeBaseService.cs          # 知识库（RAG）
|   |   |   |   +-- InkwellTools.cs                  # Function Tools
|   |   |   |   +-- ToolLoopCheckpointService.cs     # 工具循环检查点
|   |   |   |   +-- Middleware/                       # 中间件
|   |   |   |   |   +-- ContentGuardrailMiddleware.cs     # 内容安全护栏
|   |   |   |   |   +-- FunctionCallAuditMiddleware.cs    # 函数调用审计
|   |   |   |   |   +-- SessionPersistenceMiddleware.cs   # 会话持久化
|   |   |   |   +-- Skills/                          # Agent 技能
|   |   |   |       +-- ContentSkills.cs             # Markdown 校验、敏感词扫描等
|   |   |   +-- Workflows/                           # Workflow 定义
|   |   |   |   +-- ContentPipelineBuilder.cs        # 内容流水线
|   |   |   |   +-- WriterCriticLoopBuilder.cs       # Writer-Critic 循环
|   |   |   |   +-- TranslationPipelineBuilder.cs    # 翻译流水线（Fan-Out/Fan-In）
|   |   |   |   +-- TopicDiscussionBuilder.cs        # 选题讨论会（GroupChat）
|   |   |   |   +-- SmartRoutingBuilder.cs           # 智能路由（Handoff）
|   |   |   |   +-- ContentWithTranslationBuilder.cs # 内容+翻译（SubWorkflow）
|   |   |   |   +-- BatchEvaluationBuilder.cs        # 批量评估（MapReduce）
|   |   |   |   +-- Executors/                       # 13 个 Executor
|   |   |   +-- Queue/                               # InMemory 队列 + PubSub
|   |   |   +-- Storage/                             # 本地文件存储
|   |   |
|   |   +-- providers/                               # 可插拔提供程序
|   |       +-- Inkwell.Persistence.EntityFrameworkCore/  # EF Core 抽象
|   |       +-- Inkwell.Persistence.InMemory/         # InMemory 实现
|   |       +-- Inkwell.Persistence.SqlServer/        # SQL Server + Migrations
|   |       +-- Inkwell.Queue.Redis/                  # Redis Queue + PubSub
|   |
|   +-- app/                                         # 应用层
|       +-- aspire/                                  # .NET Aspire 编排
|       |   +-- Inkwell.AppHost/                     # AppHost 项目
|       |   +-- Inkwell.ServiceDefaults/             # OpenTelemetry + 健康检查
|       +-- services/                                # 微服务
|       |   +-- Inkwell.WebApi/                      # 主 API（AG-UI、REST、YAML Agent）
|       |   +-- Inkwell.A2AServer/                   # A2A 协议服务
|       |   +-- Inkwell.DurableHost/                 # DurableTask 后台主机
|       |   +-- Inkwell.Functions/                   # Azure Functions
|       +-- webapp/                                  # React SPA 前端
|
+-- tests/Inkwell.Tests/                             # 单元测试（60 个测试）
+-- docker/                                          # Docker Compose 部署
|   +-- docker-compose.yml
|   +-- nginx.conf
|   +-- .env.example
+-- docs/                                            # 设计文档
```

---

## 三、核心架构设计

### 3.1 分层架构

```
+---------------------------------------------------+
|                   应用层 (App)                      |
|   WebApi (AG-UI + REST Controllers)                |
|   Webapp (React + Ant Design X)                    |
|   A2AServer / DurableHost / Functions              |
+---------------------------------------------------+
|              Agent 层 (Agents)                      |
|   10+ Agent (Writer/Critic/Analyst/SEO/...)        |
|   Middleware (Guardrail/Audit/SessionPersistence)   |
|   Skills (Markdown/Readability/Sensitive)           |
|   Memory (VectorStore) + Knowledge (RAG)           |
+---------------------------------------------------+
|            工作流层 (Workflows)                      |
|   8 条 Workflow (Pipeline/Loop/GroupChat/...)       |
|   13 个 Executor                                    |
|   声明式 YAML Workflow + Agent                      |
+---------------------------------------------------+
|            抽象层 (Abstractions)                     |
|   IPersistenceProvider<T, TKey>                     |
|   ISessionPersistenceProvider                       |
|   IQueueProvider<T> / IPubSubProvider<T>            |
|   IFileStorageProvider                              |
+---------------------------------------------------+
|          提供程序层 (Providers)                      |
| EF Core | InMemory | SqlServer | Redis              |
+---------------------------------------------------+
|           基础设施 (Infrastructure)                  |
| Docker Compose | Aspire | OpenTelemetry | DTS      |
+---------------------------------------------------+
```

### 3.2 服务注册

```csharp
// InMemory（默认，开箱即用）
builder.Services.AddInkwellCore()
    .UseInMemoryDatabase()
    .UseInMemoryQueue()
    .UseLocalFileStorage();

// SQL Server + Redis（持久化存储）
builder.Services.AddInkwellCore()
    .UseSqlServer(connectionString)
    .UseRedisQueue(redisConnectionString)
    .UseLocalFileStorage("/data/inkwell/storage");
    // UseLocalFileStorage 基于 IFileStorageProvider 接口
    // 可替换为 Azure Blob Storage、AWS S3、MinIO 等对象存储实现
```

### 3.3 Agent 架构

```
AgentServiceCollectionExtensions.AddInkwellAgents()
  |
  +-- InkwellAgents.CreateWriter()         # Primary 模型
  +-- InkwellAgents.CreateCritic()         # Primary 模型
  +-- InkwellAgents.CreateImageAnalyst()   # Primary 模型
  +-- InkwellAgents.CreateMarketAnalyst()  # Secondary 模型
  +-- InkwellAgents.CreateCompetitorAnalyst()
  +-- InkwellAgents.CreateSeoOptimizer()   # Agent-as-Tool
  +-- InkwellAgents.CreateTranslator()     # 多语言（EN/JP）
  +-- InkwellAgents.CreateCoordinator()    # 智能调度
  |
  +-- 声明式 YAML Agent（运行时从 Agents/ 目录加载）
  +-- 声明式 YAML Workflow（运行时从 Workflows/ 目录加载）
```

**中间件管线**：

```
IChatClient --> ContentGuardrailMiddleware --> FunctionCallAuditMiddleware --> LLM
                      |                              |
                  敏感词过滤                    函数调用日志审计
                  （支持流式）
```

**AG-UI 会话持久化**：

```
AG-UI 请求 --> SessionPersistenceMiddleware --> Agent
                    |                            |
              加载 AgentSession               保存 Session + Messages
              (ISessionPersistenceProvider)    LLM 生成会话标题
```

### 3.4 持久化架构

```
IPersistenceProvider<TModel, TKey>          <-- 泛型接口（Abstractions）
    |
    +-- IArticlePersistenceProvider
    +-- IPipelineRunPersistenceProvider
    +-- IAnalysisPersistenceProvider
    +-- IReviewPersistenceProvider
    |
ISessionPersistenceProvider                 <-- 会话持久化接口（Abstractions）
    |
    +-- InMemorySessionPersistenceProvider  <-- Persistence.InMemory
    +-- EfCoreSessionPersistenceProvider    <-- Persistence.EntityFrameworkCore
    +-- EfCoreScopedSessionPersistenceProvider  <-- Singleton 包装器
```

**Model / Entity 分离**：
- **Model**（`ArticleRecord`、`SessionInfo`、`ChatMessageRecord` 等）：定义在 `Inkwell.Abstractions`
- **Entity**（`ArticleEntity`、`ChatSessionEntity` 等）：定义在 `Inkwell.Persistence.EntityFrameworkCore`
- **映射**：通过 `EfCorePersistenceProvider` 的 `ToEntity()` / `ToModel()` 抽象方法实现

### 3.5 队列架构

```
IQueueProvider<T>     <-- FIFO 队列（Enqueue / Dequeue / GetCount）
IPubSubProvider<T>    <-- 发布订阅（Publish / Subscribe）
    |
    +-- InMemoryQueueProvider<T>     <-- ConcurrentQueue（开发）
    +-- InMemoryPubSubProvider<T>    <-- 委托列表（开发）
    +-- RedisQueueProvider<T>        <-- Redis List（生产）
    +-- RedisPubSubProvider<T>       <-- Redis Pub/Sub（生产）
```

### 3.6 知识库与记忆

```
KnowledgeBaseService（RAG）
    |-- AddDocumentAsync()      # 文本/文件上传
    |-- ChunkText()             # 分块（500 字符 + 50 重叠）
    |-- EmbedChunksAsync()      # 向量嵌入
    |-- VectorSearchAsync()     # 语义搜索
    |-- KeywordSearch()         # 关键词回退
    +-- CreateSearchProvider()  # 作为 AIContextProvider 注入 Agent

AgentMemoryService（长期记忆）
    +-- CreateMemoryProvider()  # 基于 ChatHistoryMemoryProvider + VectorStore
```

---

## 四、Workflow 编排

### 4.1 Workflow 清单

| Workflow               | MAF 能力                   | 说明                             |
| ---------------------- | -------------------------- | -------------------------------- |
| ContentPipeline        | Fan-Out/Fan-In、Loop、HITL | 选题分析 -> 写作 -> 审核 -> 发布 |
| WriterCriticLoop       | Loop、Switch               | Writer-Critic 迭代循环           |
| TranslationPipeline    | Fan-Out/Fan-In             | 多语言并行翻译                   |
| TopicDiscussion        | GroupChat                  | 多 Agent 轮流讨论选题            |
| SmartRouting           | Handoff                    | 根据问题类型路由到专家 Agent     |
| ContentWithTranslation | SubWorkflow                | 内容创作 + 翻译子流程            |
| BatchEvaluation        | MapReduce、Checkpoint      | 批量文章评估                     |
| 声明式 YAML Workflow   | Declarative                | 从 YAML 文件加载                 |

### 4.2 内容流水线拓扑

```
[输入: 主题(string)]
      |
      v
InputDispatch --- Fan-Out --> MarketAnalysis ---+
                              CompetitorAnalysis--+ Fan-In Barrier
                                                   v
                                        AnalysisAggregation --> [选题分析完成]
                                                   |
                                                   v
                               +---- WriterExecutor <--------------+
                               |           |                       |
                               |           v                       | Critic 退回
                               |    CriticExecutor ----------------+
                               |           |
                               |           | Critic 通过
                               |           v
                               |    RequestPort(人工审核)
                               |           |
                               |           v
                               |    ReviewGateExecutor
                               |      |          |
                               |      |(发布)    |(退回)
                               |      v          +---> WriterExecutor
                               |   [最终输出]
```

---

## 五、API 设计

### 5.1 端点总览

| Controller | Method | Route                            | 说明                     |
| ---------- | ------ | -------------------------------- | ------------------------ |
| Health     | GET    | `/health`                        | 健康检查（Aspire 默认）  |
| Dashboard  | GET    | `/api/dashboard/stats`           | 统计数据                 |
| Agents     | GET    | `/api/agents`                    | Agent 列表               |
| Sessions   | GET    | `/api/sessions?agentId={id}`     | 会话列表                 |
| Sessions   | GET    | `/api/sessions/{id}`             | 会话详情                 |
| Sessions   | POST   | `/api/sessions`                  | 创建会话                 |
| Sessions   | PATCH  | `/api/sessions/{id}`             | 更新标题                 |
| Sessions   | DELETE | `/api/sessions/{id}`             | 删除会话                 |
| Sessions   | GET    | `/api/sessions/{id}/messages`    | 消息列表                 |
| Sessions   | GET    | `/api/sessions/{id}/export`      | Markdown 导出            |
| Sessions   | GET    | `/api/sessions/search?q={query}` | 搜索会话                 |
| Knowledge  | GET    | `/api/knowledge`                 | 知识库文档列表           |
| Knowledge  | POST   | `/api/knowledge/upload`          | 上传文本文档             |
| Knowledge  | POST   | `/api/knowledge/upload-file`     | 上传文件（txt/md）       |
| Knowledge  | GET    | `/api/knowledge/{id}/chunks`     | 文档分块详情             |
| Knowledge  | DELETE | `/api/knowledge/{id}`            | 删除文档                 |
| Articles   | GET    | `/api/articles`                  | 文章列表                 |
| Pipeline   | GET    | `/api/pipeline/runs`             | 运行记录列表             |
| Pipeline   | POST   | `/api/pipeline/run`              | 启动流水线               |
| Workflows  | GET    | `/api/workflows`                 | Workflow 列表            |
| Workflows  | GET    | `/api/workflows/{id}/topology`   | Workflow 拓扑（Mermaid） |
| AG-UI      | POST   | `/api/agui/{agentId}`            | AG-UI SSE 对话端点       |

### 5.2 AG-UI 协议

所有 Agent 和 Workflow 通过 AG-UI 协议暴露为 SSE 流式对话端点：

```
POST /api/agui/writer          # Writer Agent
POST /api/agui/critic          # Critic Agent
POST /api/agui/coordinator     # 智能调度 Agent
POST /api/agui/workflow-{id}   # Workflow（AsAIAgent 包装）
```

---

## 六、前端架构

### 6.1 技术选型

| 组件     | 技术           | 用途                |
| -------- | -------------- | ------------------- |
| UI 框架  | Ant Design 6   | 基础组件            |
| AI 对话  | Ant Design X   | Agent 对话界面      |
| 流程图   | Mermaid        | Workflow 拓扑可视化 |
| 状态管理 | Zustand        | 轻量状态管理        |
| 路由     | React Router 7 | SPA 路由            |

### 6.2 页面结构

| 路由         | 页面       | 功能                                    |
| ------------ | ---------- | --------------------------------------- |
| `/`          | Dashboard  | 统计卡片 + 最近运行                     |
| `/pipeline`  | 流水线运行 | Agent 选择 + 对话 + 会话管理            |
| `/workflow`  | Workflow   | Workflow 选择 + 拓扑可视化 + 对话式运行 |
| `/knowledge` | 知识库     | 文档列表 + 上传（文本/文件）+ 分块预览  |

---

## 七、部署架构

### 7.1 Docker Compose（7 个服务）

```
+-----------+     +----------+     +-----------+
| Webapp    |---->| WebApi   |---->| SQL Server|
| :3000     |     | :5000    |     | :1433     |
| (nginx)   |     | (.NET)   |     |           |
+-----------+     +----+-----+     +-----------+
                       | OTLP
                  +----+-----+     +-----------+
                  | Aspire   |     | DTS       |
                  | Dashboard|     | Emulator  |
                  | :18888   |     | :8080/8082|
                  +----------+     +-----------+
                  +----------+     +-----------+
                  | A2A      |     | Durable   |
                  | Server   |     | Host      |
                  | :5100    |     | (Worker)  |
                  +----------+     +-----------+
```

### 7.2 三种运行方式

| 方式           | 说明                                   |
| -------------- | -------------------------------------- |
| Docker Compose | `docker compose up -d`，全部容器化     |
| 本地开发       | `dotnet run` + `npm run dev`           |
| .NET Aspire    | `dotnet run --project Inkwell.AppHost` |

---

## 八、已实现 MAF 能力覆盖

| MAF 能力            | Inkwell 使用                              |
| ------------------- | ----------------------------------------- |
| Fan-Out / Fan-In    | 市场分析 + 竞品分析并行、多语言翻译并行   |
| Loop                | Writer-Critic 迭代循环                    |
| Switch 条件路由     | Critic 审核结果路由、ReviewGate 发布/退回 |
| Human-in-the-Loop   | RequestPort 暂停等待人工终审              |
| SharedState         | 分析报告和文章在 Executor 间共享          |
| GroupChat           | 选题讨论会（多 Agent 轮流讨论）           |
| Handoff             | 智能路由（根据问题类型切换专家 Agent）    |
| SubWorkflow         | 内容创作 + 翻译子流程组合                 |
| MapReduce           | 批量文章评估                              |
| Checkpoint          | 工具循环检查点保存/恢复                   |
| Function Tools      | 搜索工具、结构化输出                      |
| Agent-as-Tool       | SEO Agent 作为 Coordinator 的函数工具     |
| 声明式 YAML         | YAML 定义 Agent 和 Workflow，运行时加载   |
| AsAIAgent()         | 所有 Workflow 包装为 Agent + AG-UI 端点   |
| AG-UI 协议          | SSE 流式对话、会话管理                    |
| ChatHistoryMemory   | VectorStore 语义检索跨会话记忆            |
| Middleware Pipeline | 内容安全护栏、函数调用审计、会话持久化    |
| Skills              | Markdown 校验、可读性分析、敏感词扫描     |
| DurableTask         | DTS Emulator + DurableHost 持久化编排     |
