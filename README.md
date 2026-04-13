# Inkwell

**AI 驱动的内容生产平台** -- 基于 [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 构建

## 项目目标

通过一个完整的内容生产业务场景，系统性覆盖 Microsoft Agent Framework (MAF) 的核心能力，包括多 Agent 协作、Workflow 编排、对话持久化、长期记忆、流式交互等，同时构建一个可运行的全栈应用。

## 技术栈

| 层级     | 技术                                                                         |
| -------- | ---------------------------------------------------------------------------- |
| 后端     | .NET 10 / ASP.NET Core                                                       |
| AI 编排  | Microsoft Agent Framework 1.1.0                                              |
| LLM      | Microsoft.Extensions.AI + Azure OpenAI                                       |
| 持久化   | Entity Framework Core (InMemory / SQL Server)                                |
| 向量存储 | Microsoft.Extensions.VectorData + InMemory (可切换 Qdrant / Azure AI Search) |
| 前端     | React 19 + TypeScript + Ant Design 6 + Ant Design X                          |

## 已实现功能

### Agent 能力

- **10+ 预定义 Agent**: 内容写手、内容审核、市场分析、竞品分析、SEO 优化、图片分析、智能调度、多语言翻译等
- **Function Tools**: 搜索工具、关键词分析、文章发布（带审批）
- **结构化输出**: TopicAnalysis、SeoReport 等 JSON Schema 输出
- **Agent-as-Tool**: Coordinator 通过函数接口调用 SEO Agent
- **声明式 Agent**: YAML 定义，自动加载注册
- **中间件管线**: 内容安全护栏（支持流式）、函数调用审计
- **Skills**: Markdown 校验、可读性分析、敏感词扫描

### Workflow 编排

- **8 条 Workflow**: 内容流水线、翻译流水线、Writer-Critic 循环、选题讨论会、智能路由、批量评估、内容+翻译一体化、声明式审批流
- **编排模式**: Fan-Out / Fan-In、Switch 条件路由、Human-in-the-Loop、GroupChat、Handoff、SubWorkflow、MapReduce、Checkpoint
- **Workflow as Agent**: 所有 Workflow 通过 `AsAIAgent()` + AG-UI 暴露为对话式端点

### 对话与记忆

- **会话持久化**: 基于 MAF `AgentSession` 序列化机制，跨请求保持对话上下文
- **聊天裁剪**: 对话型 Agent 使用 Pipeline 策略（工具结果压缩 -> 摘要 -> 截断），任务型使用消息计数裁剪
- **长期记忆**: `ChatHistoryMemoryProvider` 基于向量存储的跨会话语义检索
- **会话管理 API**: 列表、详情、消息、搜索、重命名、删除、Markdown 导出

### 前端交互

- **AG-UI 流式对话**: 基于 SSE 的逐字流式输出，Markdown 渲染
- **会话侧栏**: 历史会话列表、搜索、切换、重命名、删除、导出
- **Workflow 管理**: Mermaid 拓扑图可视化、AGUI 对话式运行
- **统一组件体系**: AguiChatPanel / ConversationShell / SessionSidebar 等可复用组件

### 基础设施

- **多模型服务**: Primary / Secondary Keyed IChatClient
- **可插拔持久化**: InMemory / SQL Server / EF Core
- **可插拔向量存储**: InMemory (可切换 Qdrant / Azure AI Search)
- **认证授权**: JWT + 角色策略
- **OpenTelemetry**: ASP.NET Core + HTTP + Workflow 追踪
- **DurableTask 托管**: Console + Azure Functions 项目
- **A2A 服务器**: 骨架项目

## 项目结构

```
Inkwell/
  src/
    core/
      Inkwell.Abstractions/          # 接口、模型、Builder
      Inkwell.Core/                  # Agent、Workflow、中间件、记忆服务
      providers/                     # EF Core、InMemory、SqlServer、Redis
    app/
      webapi/Inkwell.WebApi/         # ASP.NET Core Web API + AG-UI 端点
      webapp/                        # React 前端 (Vite + Ant Design)
      functions/                     # Azure Functions 托管
      durable-host/                  # DurableTask Console 托管
      a2a-server/                    # A2A 服务器
  docs/
    architecture/                    # 架构文档
    design/                          # 设计方案（会话持久化、向量存储）
    plan/                            # 实施计划、需求文档
```

## 快速开始

### 前置要求

- .NET 10 SDK
- Azure OpenAI 服务（Chat Completion + Embedding 部署）
- Node.js 20+

### 配置

```bash
cd src/app/webapi/Inkwell.WebApi
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Primary:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Primary:DeploymentName" "gpt-4o"
dotnet user-secrets set "AzureOpenAI:Primary:ApiKey" "your-api-key"
```

### 运行

```bash
# 后端
dotnet run --project src/app/webapi/Inkwell.WebApi

# 前端
cd src/app/webapp
npm install
npm run dev
```

访问 http://localhost:5188

## 许可证

MIT
# Inkwell

**AI 驱动的内容生产平台** — 基于 [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 构建

Inkwell 是一个端到端的 AI 内容生产流水线，将选题分析、内容创作、质量审核和人工终审编排为自动化工作流。项目的目标是通过真实业务场景，完整覆盖 Microsoft Agent Framework（MAF）的核心能力。

## 快速开始

### 前置要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)（用于认证）
- Azure OpenAI 服务（需要一个 Chat Completion 部署）

### 配置

```bash
cd src/app/console/Inkwell.ConsoleApp

# 初始化 user-secrets
dotnet user-secrets init

# 配置 Azure OpenAI
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AZURE_OPENAI_DEPLOYMENT_NAME" "gpt-4o-mini"
```

### 运行

```bash
# 从仓库根目录
dotnet run --project src/app/console/Inkwell.ConsoleApp -- "The Future of AI in Healthcare"

# 或交互模式（不传参数）
dotnet run --project src/app/console/Inkwell.ConsoleApp
```

### 运行效果

```
========== Inkwell 内容生产流水线 ==========
主题: The Future of AI in Healthcare
============================================

  [InputDispatch] 完成
  [CompetitorAnalysis] 完成
  [MarketAnalysis] 完成
  [AnalysisAggregation] 完成

[输出] [选题分析完成] 主题: The Future of AI in Healthcare
趋势: Most competitor content focuses on broad AI adoption...

  [Writer] 完成
  [Critic] 完成

╔══════════════════════════════════════╗
║         人工审核                      ║
╚══════════════════════════════════════╝
标题: The Future of AI in Healthcare
版本: 第 1 稿
状态: Approved
─────────────────────────────────────
(文章内容...)
─────────────────────────────────────
是否批准发布？(y/n): y
→ 已批准发布

[输出] [已发布] The Future of AI in Healthcare
(最终文章...)

========== 流水线执行完毕 ==========
```

## 架构

### Workflow 拓扑

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

### MAF 能力覆盖

| MAF 能力             | Inkwell 中的使用                                          |
| -------------------- | --------------------------------------------------------- |
| Fan-Out / Fan-In     | 市场分析 + 竞品分析并行，Barrier 汇聚                     |
| Writer-Critic Loop   | 写作 → 审核 → 退回修改循环                                |
| AddSwitch 条件路由   | Critic 根据审核结果路由（通过/退回）                      |
| Human-in-the-Loop    | RequestPort 暂停等待人工终审                              |
| SharedState 共享状态 | 分析报告和文章在 Executor 间共享                          |
| YieldOutputAsync     | 两阶段输出（分析完成 + 最终发布）                         |
| 结构化输出           | Agent 使用 JsonSchema 返回 TopicAnalysis / ReviewDecision |

### 项目结构

```
Inkwell/
├── src/
│   ├── core/
│   │   ├── Inkwell.Abstractions/       # 接口 + 模型
│   │   ├── Inkwell.Core/              # 默认实现（InMemory 队列、本地文件存储）
│   │   ├── Inkwell.Workflows/         # MAF Workflow 定义和 Executor
│   │   └── providers/                  # 可插拔提供程序
│   │       ├── Inkwell.Persistence.EntityFrameworkCore/
│   │       ├── Inkwell.Persistence.InMemory/
│   │       ├── Inkwell.Persistence.SqlServer/
│   │       └── Inkwell.Queue.Redis/
│   └── app/
│       ├── webapi/Inkwell.WebApi/      # ASP.NET Core Web API
│       └── webapp/                     # React 前端
├── docs/
│   ├── architecture.md                 # 架构设计文档
│   └── requirements.md                 # 需求文档
├── Inkwell.slnx
├── Directory.Build.props
├── Directory.Packages.props
└── README.md
```

## 开发路线

| Phase       | 内容                                                | 状态     |
| ----------- | --------------------------------------------------- | -------- |
| **Phase 1** | 选题分析 + 内容创作 + 人工审核                      | ✅ 已完成 |
| **Phase 2** | 多语言翻译（Fan-Out）+ SEO 工具 + 条件发布路由      | 🔜 计划中 |
| **Phase 3** | OpenTelemetry 可观测性 + Workflow 可视化            | 🔜 计划中 |
| **Phase 4** | 声明式 YAML 模板 + Workflow as Agent API            | 🔜 计划中 |
| **Phase 5** | GroupChat 选题会 + SubworkflowBinding + DurableTask | 🔜 计划中 |

## 技术栈

- [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 1.1.0
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) 10.4.1
- [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/) (gpt-4o-mini / gpt-4.1)
- .NET 10

## 许可证

MIT
