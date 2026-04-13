# Inkwell 架构设计文档

## 一、系统概述

Inkwell 是一个 AI 驱动的内容生产平台，基于 Microsoft Agent Framework（MAF）编排多个 AI Agent 和人工环节，实现从选题到发布的端到端自动化。

### 技术栈

| 层级       | 技术                                                |
| ---------- | --------------------------------------------------- |
| 后端框架   | .NET 10 / ASP.NET Core                              |
| AI 编排    | Microsoft Agent Framework 1.1.0                     |
| LLM 客户端 | Microsoft.Extensions.AI + Azure OpenAI              |
| 持久化     | Entity Framework Core 10（InMemory / SQL Server）   |
| 队列       | InMemory / Redis（StackExchange.Redis）             |
| 前端       | React 19 + TypeScript + Ant Design 6 + Ant Design X |
| 流程图     | React Flow                                          |

---

## 二、项目结构

```
Inkwell/
├── src/
│   ├── core/                                    # 核心层
│   │   ├── Inkwell.Abstractions/               # 接口定义 + 模型
│   │   │   ├── Models/                            # 数据模型（Record + 业务模型）
│   │   │   ├── Persistence/                       # 持久化接口
│   │   │   ├── Queue/                             # 队列接口
│   │   │   ├── Storage/                           # 文件存储接口
│   │   │   ├── IPersistenceProvider.cs            # 泛型 CRUD 接口
│   │   │   ├── InkwellCoreBuilder.cs              # 服务构建器
│   │   │   └── InkwellServiceCollectionExtensions.cs
│   │   │
│   │   ├── Inkwell.Core/                       # 默认 InMemory 实现
│   │   │   ├── Queue/                             # InMemory 队列 + PubSub
│   │   │   └── Storage/                           # 本地文件存储
│   │   │
│   │   ├── Inkwell.Workflows/                  # MAF Workflow 定义
│   │   │   ├── Executors/                         # 7 个 Executor
│   │   │   └── ContentPipelineBuilder.cs          # Workflow 构建器
│   │   │
│   │   └── providers/                           # 可插拔提供程序
│   │       ├── Inkwell.Persistence.EntityFrameworkCore/  # EF Core 抽象
│   │       │   ├── Entities/                      # 数据库实体（带 Attribute）
│   │       │   ├── InkwellDbContext.cs             # DbContext
│   │       │   └── EfCore*Provider.cs             # Entity ↔ Model 映射
│   │       ├── Inkwell.Persistence.InMemory/      # EF Core InMemory
│   │       ├── Inkwell.Persistence.SqlServer/     # SQL Server + Migrations
│   │       └── Inkwell.Queue.Redis/               # Redis Queue + PubSub
│   │
│   └── app/                                     # 应用层
│       ├── webapi/Inkwell.WebApi/              # ASP.NET Core Web API
│       │   └── Controllers/                       # Controller + Action
│       └── webapp/                              # React 前端
│           └── src/
│               ├── features/pipeline/             # 流水线运行页
│               ├── features/dashboard/            # Dashboard
│               └── stores/                        # Zustand 状态管理
│
├── docs/
│   ├── architecture.md                          # 本文档
│   └── requirements.md                          # 需求文档
└── README.md
```

---

## 三、核心架构设计

### 3.1 分层架构

```
┌─────────────────────────────────────────┐
│              应用层 (App)                │
│     WebApi (Controller + Action)        │
│     Webapp (React + Ant Design X)       │
├─────────────────────────────────────────┤
│            工作流层 (Workflows)          │
│     ContentPipelineBuilder              │
│     7 个 Executor                       │
│     MAF WorkflowBuilder API            │
├─────────────────────────────────────────┤
│            抽象层 (Abstractions)         │
│     IPersistenceProvider<T, TKey>       │
│     IQueueProvider<T>                   │
│     IPubSubProvider<T>                  │
│     IFileStorageProvider                │
│     InkwellCoreBuilder                  │
├─────────────────────────────────────────┤
│          提供程序层 (Providers)          │
│ EF Core │ InMemory │ SqlServer │ Redis  │
│            Local File Storage           │
└─────────────────────────────────────────┘
```

### 3.2 服务注册（Fluent API）

```csharp
// 开发环境
builder.Services.AddInkwellCore()
    .UseInMemoryDatabase()
    .UseInMemoryQueue()
    .UseLocalFileStorage();

// 生产环境
builder.Services.AddInkwellCore()
    .UseSqlServer(connectionString)
    .UseRedisQueue(redisConnectionString)
    .UseLocalFileStorage("/data/inkwell/storage");

// 配置文件驱动
builder.Services.AddInkwellCore()
    .UseConfiguredPersistence(builder.Configuration)
    .UseLocalFileStorage();
```

`InkwellCoreBuilder` 作为链式构建的载体，所有 `UseXxx` 扩展方法都在对应的 Provider 包中定义，返回 `InkwellCoreBuilder` 支持继续链式调用。

### 3.3 持久化架构

```
IPersistenceProvider<TModel, TKey>          ← 泛型接口（Abstractions）
    │
    ├── IArticlePersistenceProvider         ← 领域接口（Abstractions）
    ├── IPipelineRunPersistenceProvider
    ├── IAnalysisPersistenceProvider
    └── IReviewPersistenceProvider
            │
            ▼
EfCorePersistenceProvider<TEntity, TModel, TKey>  ← EF Core 抽象基类
    │   ├── ToEntity(model) → entity       ← 双向映射
    │   └── ToModel(entity) → model
    │
    ├── EfCoreArticlePersistenceProvider    ← 具体实现
    ├── EfCorePipelineRunPersistenceProvider
    ├── EfCoreAnalysisPersistenceProvider
    └── EfCoreReviewPersistenceProvider
```

**Model / Entity 分离**：
- **Model**（`ArticleRecord` 等）：数据载体，定义在 `Inkwell.Abstractions`，无 EF Core 依赖
- **Entity**（`ArticleEntity` 等）：数据库实体，定义在 `Inkwell.Persistence.EntityFrameworkCore`，带 `[Table]`/`[Key]`/`[Required]`/`[MaxLength]` 等 Data Annotation
- **映射**：通过 `EfCorePersistenceProvider` 的 `ToEntity()` / `ToModel()` 抽象方法实现双向转换

**索引策略**：Entity Attribute 优先，FluentAPI 仅补充索引（`HasIndex`）。

### 3.4 队列架构

```
IQueueProvider<T>     ← FIFO 队列（Enqueue / Dequeue / GetCount）
IPubSubProvider<T>    ← 发布订阅（Publish / Subscribe）
    │
    ├── InMemoryQueueProvider<T>     ← ConcurrentQueue（开发）
    ├── InMemoryPubSubProvider<T>    ← 委托列表（开发）
    │
    ├── RedisQueueProvider<T>        ← Redis List（生产）
    └── RedisPubSubProvider<T>       ← Redis Pub/Sub（生产）
```

### 3.5 Workflow 拓扑

```
[输入: 主题(string)]
      │
      ▼
InputDispatch ─── Fan-Out ──▶ MarketAnalysis ──┐
                             ▶ CompetitorAnalysis──┤ Fan-In Barrier
                                                    ▼
                                         AnalysisAggregation ──▶ [输出: 选题分析完成]
                                                    │
                                                    ▼
                                ┌──── WriterExecutor ◀──────────┐
                                │           │                   │
                                │           ▼                   │ Critic 退回
                                │    CriticExecutor ────────────┘
                                │           │
                                │           │ Critic 通过
                                │           ▼
                                │    RequestPort(人工审核) ⏸
                                │           │
                                │           ▼
                                │    ReviewGateExecutor
                                │      │          │
                                │      │(发布)    │(退回)
                                │      ▼          └──▶ WriterExecutor
                                │   [最终输出]
```

**MAF 能力覆盖**：

| MAF 能力           | Inkwell 使用                         |
| ------------------ | ------------------------------------ |
| Fan-Out / Fan-In   | 市场分析 + 竞品分析并行              |
| Writer-Critic Loop | 写作 → 审核 → 退回修改循环           |
| AddSwitch 条件路由 | Critic 根据审核结果路由              |
| Human-in-the-Loop  | RequestPort 暂停等待人工终审         |
| SharedState        | 分析报告和文章在 Executor 间共享     |
| 结构化输出         | Agent 使用 JsonSchema 返回结构化数据 |

---

## 四、API 设计

### 4.1 端点总览

| Controller | Method | Route                           | 说明              |
| ---------- | ------ | ------------------------------- | ----------------- |
| Health     | GET    | `/api/health`                   | 健康检查          |
| Dashboard  | GET    | `/api/dashboard/stats`          | 统计数据          |
| Articles   | GET    | `/api/articles`                 | 文章列表          |
| Articles   | GET    | `/api/articles/{id}`            | 文章详情          |
| Articles   | GET    | `/api/articles/status/{status}` | 按状态查询        |
| Pipeline   | GET    | `/api/pipeline/runs`            | 运行记录列表      |
| Pipeline   | GET    | `/api/pipeline/runs/{id}`       | 运行记录详情      |
| Pipeline   | POST   | `/api/pipeline/run`             | 启动流水线（SSE） |
| Reviews    | GET    | `/api/reviews/{articleId}`      | 审核记录          |
| Analyses   | GET    | `/api/analyses/{pipelineRunId}` | 分析报告          |

### 4.2 SSE 事件格式

`POST /api/pipeline/run` 返回 `text/event-stream`，事件格式：

```
data: {"type":"executor_complete","runId":"...","executorId":"MarketAnalysis","timestamp":...}

data: {"type":"output","runId":"...","data":"[选题分析完成] ...","executorId":"AnalysisAggregation","timestamp":...}

data: {"type":"human_review_request","runId":"...","requestId":"...","timestamp":...}

data: {"type":"done","runId":"..."}
```

---

## 五、前端架构

### 5.1 技术选型

| 组件     | 技术           | 用途                |
| -------- | -------------- | ------------------- |
| UI 框架  | Ant Design 6   | 基础组件            |
| AI 对话  | Ant Design X   | Agent 对话界面      |
| 流程图   | React Flow     | Workflow 拓扑可视化 |
| 状态管理 | Zustand        | 轻量状态管理        |
| 路由     | React Router 7 | SPA 路由            |

### 5.2 页面结构

| 路由            | 页面       | 功能                             |
| --------------- | ---------- | -------------------------------- |
| `/`             | Dashboard  | 统计卡片 + 最近运行              |
| `/pipeline/run` | 流水线运行 | 输入主题 + 执行时间线 + 人工审核 |

---

## 六、扩展路线

| Phase     | 内容                             | MAF 能力                                        |
| --------- | -------------------------------- | ----------------------------------------------- |
| Phase 1 ✅ | 选题分析 + 内容创作 + 人工审核   | Fan-Out/Fan-In、Loop、Switch、HITL、SharedState |
| Phase 2   | 多语言翻译 + SEO 工具 + 条件发布 | 并行、Function Tool、条件路由、Checkpoint       |
| Phase 3   | OpenTelemetry + Visualization    | WithOpenTelemetry、ToMermaidString              |
| Phase 4   | YAML 模板 + Workflow as Agent    | Declarative、AsAIAgent                          |
| Phase 5   | GroupChat + SubworkflowBinding   | GroupChat、Handoff、DurableTask                 |