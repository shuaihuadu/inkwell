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
│   │   ├── Inkwell.Core/              # 业务模型（Article, TopicAnalysis, ReviewDecision）
│   │   └── Inkwell.Workflows/         # Workflow 定义和 Executor
│   │       ├── Executors/
│   │       │   ├── InputDispatchExecutor.cs
│   │       │   ├── MarketAnalysisExecutor.cs
│   │       │   ├── CompetitorAnalysisExecutor.cs
│   │       │   ├── AnalysisAggregationExecutor.cs
│   │       │   ├── WriterExecutor.cs
│   │       │   ├── CriticExecutor.cs
│   │       │   └── ReviewGateExecutor.cs
│   │       └── ContentPipelineBuilder.cs
│   └── app/
│       └── console/
│           └── Inkwell.ConsoleApp/     # 控制台入口
├── docs/
│   └── requirements.md                # 需求文档
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
