# Inkwell 需求文档

## 项目定位

Inkwell 是一个 AI 驱动的内容生产与智能体管理平台，基于 Microsoft Agent Framework（MAF）构建。项目的目标是：

1. **覆盖 MAF 全部应用场景**：AIAgent、Workflow、GroupChat、Handoff、HITL、可观测性、声明式等
2. **端到端可运行**：从 AI Agent 对话到 Workflow 编排到文章发布，形成完整闭环
3. **生产级架构**：可插拔持久化、队列、文件存储，集成 Aspire 可观测性

---

## 一、基础设施

### 1.1 LLM 配置

**需求**：通过 `appsettings.json` 配置 Azure OpenAI 的连接信息，支持 user-secrets 覆盖。

**配置格式**：

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

### 1.2 持久化

**需求**：通过 Fluent API 或配置文件切换持久化后端。

```csharp
builder.Services.AddInkwellCore()
    .UseInMemoryDatabase()       // 开发
    .UseSqlServer(connectionString)  // 生产
```

**覆盖的实体**：文章（ArticleRecord）、运行记录（PipelineRunRecord）、分析报告（AnalysisRecord）、审核记录（ReviewRecord）

### 1.3 队列

**需求**：FIFO 队列 + 发布/订阅，支持 InMemory 和 Redis。

```csharp
builder.Services.AddInkwellCore()
    .UseInMemoryQueue()          // 开发
    .UseRedisQueue(redisConn)    // 生产
```

### 1.4 文件存储

**需求**：本地文件系统存储（默认），后续可扩展 Azure Blob Storage。

```csharp
builder.Services.AddInkwellCore()
    .UseLocalFileStorage("storage")
```

### 1.5 可观测性（Aspire 集成）

**需求**：集成 .NET Aspire，提供 OpenTelemetry Trace/Metrics/Logs 的统一管理。

**验收标准**：
- WebApi 项目集成 Aspire ServiceDefaults
- Workflow 执行通过 `WithOpenTelemetry` 注入 Trace
- 每个 Agent 的调用、Executor 的执行、SuperStep 的推进都有对应的 Span
- Aspire Dashboard（`http://localhost:18888`）可查看完整调用链
- 支持 `EnableSensitiveData` 配置（开发环境开启，生产环境关闭）

**MAF 能力**：`WithOpenTelemetry`、`WorkflowTelemetryOptions`

---

## 二、AIAgent

### 2.1 预定义 Agent

**需求**：系统内置多个专业 Agent，每个 Agent 独立可用，也可作为 Workflow 节点。

| Agent ID | 名称 | 职责 | AG-UI 路由 |
| --- | --- | --- | --- |
| `writer` | 内容写手 | 撰写高质量文章 | `/api/agui/writer` |
| `critic` | 内容审核 | 审核文章质量，给出改进建议 | `/api/agui/critic` |
| `market-analyst` | 市场分析 | 分析市场趋势和目标受众 | `/api/agui/market-analyst` |
| `seo-optimizer` | SEO 优化 | 分析和优化搜索引擎排名 | `/api/agui/seo-optimizer` |
| `translator-english` | 英文翻译 | 翻译为英文 | `/api/agui/translator-english` |
| `translator-japanese` | 日文翻译 | 翻译为日文 | `/api/agui/translator-japanese` |

**验收标准**：
- 每个 Agent 通过 AG-UI 协议（SSE）独立对外暴露
- 前端可选择 Agent 进行对话
- Agent 注册表（`AgentRegistry`）提供元数据查询 API
- 每个 Agent 的 Instructions 使用中文

### 2.2 Agent 注册与管理

**需求**：`AgentRegistry` 作为 Agent 的集中管理入口。

**API**：
- `GET /api/agents` — 获取所有 Agent 列表
- `GET /api/agents/{id}` — 获取 Agent 详情

---

## 三、Workflow

### 3.1 内容生产流水线（串行 + 并行 + HITL）

**需求**：完整的内容生产流水线，组合多种 Workflow 编排模式。

**拓扑**：

```
输入(主题) → InputDispatch
              ├── Fan-Out → MarketAnalysis ──┐
              └── Fan-Out → CompetitorAnalysis──┤ Fan-In Barrier
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

### 3.2 多语言翻译流水线（纯并行）

**需求**：一篇文章同时翻译为多种语言。

**拓扑**：

```
输入(文章) → Fan-Out → TranslatorEN ──┐
                     → TranslatorJA ──┤ Fan-In Barrier
                     → TranslatorFR ──┘
                            ▼
                     TranslationAggregator → [输出: 多语言版本]
```

**MAF 能力**：Fan-Out / Fan-In、Agent 绑定（`BindAsExecutor`）

### 3.3 选题讨论会（GroupChat）

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

**MAF 能力**：`GroupChatWorkflowBuilder`、`GroupChatManager`、`SelectNextAgentAsync`

### 3.4 客服场景（Handoff）

**需求**：用户提问后根据问题类型自动切换到对应专业 Agent，处理完后可返回主 Agent。

**角色**：
- 协调者 Agent：初始接待，判断问题类型
- 内容写作 Agent：处理写作相关问题
- SEO Agent：处理 SEO 相关问题
- 翻译 Agent：处理翻译相关问题

**验收标准**：
- 使用 `AgentWorkflowBuilder.CreateHandoffBuilderWith` 构建
- `WithHandoff(from, to, reason)` 定义切换关系
- `EnableReturnToPrevious()` 允许返回上一个 Agent
- 切换过程对用户透明

**MAF 能力**：`HandoffWorkflowBuilder`（`[Experimental]`）

### 3.5 Workflow 可视化

**需求**：所有 Workflow 支持导出拓扑图。

**验收标准**：
- `ToMermaidString()` 导出 Mermaid 格式
- `ToDotString()` 导出 Graphviz DOT 格式
- 前端页面展示 Workflow 拓扑（使用 React Flow 或 Mermaid 渲染）

**MAF 能力**：`Visualization`

---

## 四、前端

### 4.1 Dashboard

**需求**：系统总览页。

**展示内容**：
- Agent 数量
- Workflow 数量
- 流水线运行次数 / 完成次数
- 文章总数 / 已发布数
- 审核通过率
- 最近运行记录列表

### 4.2 Agent 对话

**需求**：选择任意 Agent 进行对话。

**UI 组件**：
- Ant Design X `Bubble` 组件渲染对话
- Ant Design X `Sender` 组件输入
- Agent 选择器（下拉框）
- 新对话按钮

**对接方式**：通过 AG-UI 协议（SSE）对接后端 Agent

### 4.3 Workflow 管理

**需求**：展示系统中的 Workflow 列表，可触发运行。

**功能**：
- Workflow 列表（名称、描述、节点数、状态）
- 运行 Workflow（输入参数 → SSE 实时事件流）
- 人工审核界面（HITL 场景）
- Workflow 拓扑图展示

---

## 五、API 总览

### REST API

| Controller | Method | Route | 说明 |
| --- | --- | --- | --- |
| Health | GET | `/api/health` | 健康检查 |
| Dashboard | GET | `/api/dashboard/stats` | Dashboard 统计 |
| Agents | GET | `/api/agents` | Agent 列表 |
| Agents | GET | `/api/agents/{id}` | Agent 详情 |
| Articles | GET | `/api/articles` | 文章列表 |
| Articles | GET | `/api/articles/{id}` | 文章详情 |
| Articles | GET | `/api/articles/status/{status}` | 按状态查询 |
| Pipeline | GET | `/api/pipeline/runs` | 运行记录列表 |
| Pipeline | GET | `/api/pipeline/runs/{id}` | 运行记录详情 |
| Pipeline | POST | `/api/pipeline/run` | 启动流水线（SSE） |
| Reviews | GET | `/api/reviews/{articleId}` | 审核记录 |
| Analyses | GET | `/api/analyses/{pipelineRunId}` | 分析报告 |

### AG-UI 端点

通过 `MapAGUI` 自动注册，每个 Agent 一个端点：

```
POST /api/agui/{agent-id}
```

---

## 六、非功能性需求

### 安全
- 所有密钥通过 user-secrets 或环境变量管理
- `ApiKey` 支持配置文件，不硬编码
- `EnableSensitiveData` 仅在开发环境启用
- 文件存储防路径遍历攻击

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

### 可观测性
- 集成 Aspire ServiceDefaults
- OpenTelemetry Trace 覆盖 Agent 调用和 Workflow 执行
- Aspire Dashboard 开发环境默认启用
