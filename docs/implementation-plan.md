# Inkwell 实施计划

## 已完成

- 核心抽象层（Abstractions：接口、模型、Builder）
- 持久化提供商（InMemory / SqlServer / EF Core）
- 队列提供商（InMemory / Redis）
- 文件存储（Local）
- 6 个基础 Agent + AgentRegistry
- ContentPipelineBuilder（7 Executors，已验证通过）
- WebApi（7 Controllers + AG-UI 端点）
- 前端脚手架（Dashboard + Pipeline + AG-UI 对话）

---

## Phase 1：基础设施加固

> 所有后续 Phase 的地基。

| 任务           | 需求 | 内容                                                            |
| -------------- | ---- | --------------------------------------------------------------- |
| LLM 配置规范化 | 1.1  | appsettings.json 配置节 + Options 模式 + ApiKey/Credential 回退 |
| 多模型服务     | 1.2  | Primary / Secondary / Embedding 三个 Keyed IChatClient          |
| Aspire 集成    | 1.6  | ServiceDefaults + OpenTelemetry + Aspire Dashboard              |

**验收**：`dotnet run` 启动后 Aspire Dashboard 可看到 Trace，Agent 按模型分级。

---

## Phase 2：Agent 核心能力

> Agent 对话体验完善，前端可正常使用。

| 任务              | 需求 | 内容                                             |
| ----------------- | ---- | ------------------------------------------------ |
| 新增 3 个 Agent   | 2.1  | competitor-analyst / image-analyst / coordinator |
| DI + Context      | 2.1  | Agent 构造函数注入 + MessageAIContextProvider    |
| Function Tools    | 2.3  | 搜索资讯 / 关键词分析 / 发布审批工具             |
| 结构化输出        | 2.4  | TopicAnalysis / SeoReport 类型化返回             |
| Agent 作为工具    | 2.14 | Writer 调 SEO（进程内）                          |
| 前端 Agent 对话页 | 5.2  | 图片上传 / 工具调用展示 / AG-UI 高级功能         |

**验收**：9 个 Agent 全部可通过前端对话，Function Tool 调用可见。

---

## Phase 3：Workflow 核心编排

> 4 条 Workflow + 流式输出 + 拓扑可视化。

| 任务                 | 需求       | 内容                                 |
| -------------------- | ---------- | ------------------------------------ |
| 内容流水线增强       | 3.1 / 3.10 | SharedState + YieldOutput 实时输出   |
| 翻译流水线           | 3.2        | Fan-Out / Fan-In + BindAsExecutor    |
| Writer-Critic 循环   | 3.3        | 独立 Loop Workflow                   |
| 批量评估 MapReduce   | 3.4        | 动态 Fan-Out + AddMultiSelection     |
| 流式输出             | 3.5        | WatchStreamAsync → SSE               |
| Workflow 可视化      | 3.14       | ToMermaidString / ToDotString        |
| 前端 Workflow 管理页 | 5.3        | 列表 / 运行 / 实时进度 / HITL / 拓扑 |

**验收**：4 条 Workflow 可从前端触发运行，实时事件流 + 拓扑图。

---

## Phase 4：Agent 智能增强

> 记忆 + 知识库 + 安全 + 压缩。

| 任务           | 需求 | 内容                                         |
| -------------- | ---- | -------------------------------------------- |
| 记忆           | 2.5  | ChatHistoryMemoryProvider + VectorStore      |
| RAG            | 2.6  | TextSearchProvider + 知识库                  |
| 图像/多模态    | 2.9  | image-analyst 处理图片输入                   |
| 中间件         | 2.10 | GuardrailMiddleware + FunctionCallMiddleware |
| 对话压缩       | 2.11 | ChatReducer + CompactionPipeline             |
| 对话持久化     | 2.15 | PersistedConversationProvider → SQL          |
| 前端知识库管理 | 5.4  | 上传 / 删除 / 向量化状态                     |

**验收**：Agent 具备长期记忆 + RAG 增强 + 内容护栏 + 长对话自动压缩。

---

## Phase 5：Agent 扩展能力

> 高级 Agent 模式。

| 任务           | 需求 | 内容                            |
| -------------- | ---- | ------------------------------- |
| Skills         | 2.7  | Markdown lint / 可读性 / 敏感词 |
| MCP 集成       | 2.8  | CMS MCP 服务器                  |
| 声明式 Agent   | 2.12 | YAML 定义 + AgentRegistry 注册  |
| 后台响应       | 2.13 | ContinuationToken + 进度轮询    |
| 工具循环检查点 | 2.16 | 长链搜索断点恢复                |

**验收**：Agent 能力全覆盖（2.1 → 2.16）。

---

## Phase 6：Workflow 高级编排

> 覆盖 MAF 所有 Workflow 能力。

| 任务            | 需求 | 内容                             |
| --------------- | ---- | -------------------------------- |
| 子工作流        | 3.6  | 翻译子流程嵌套（BindAsExecutor） |
| Workflow→Agent  | 3.7  | AsAIAgent() + AG-UI 端点         |
| 混合 Workflow   | 3.8  | 确认 3.1 覆盖并显式标注          |
| Checkpoint      | 3.9  | CheckpointManager + 持久化恢复   |
| GroupChat       | 3.11 | 选题讨论会（4 角色 + Manager）   |
| Handoff         | 3.12 | 智能路由（Coordinator 分发）     |
| 声明式 Workflow | 3.13 | YAML 快速审批流                  |

**验收**：Workflow 能力全覆盖（3.1 → 3.14），7 条 Workflow 可运行。

---

## Phase 7：安全与调试

> 生产就绪。

| 任务     | 需求 | 内容                            |
| -------- | ---- | ------------------------------- |
| JWT 授权 | 1.7  | admin / editor / reviewer 角色  |
| DevUI    | 5.5  | Prompt 查看 / Token 统计 / 日志 |
| 前端打磨 | —    | 错误处理 / Loading / 数据完善   |

**验收**：API 全部需认证（开发环境可禁用），DevUI 可调试所有 Agent。

---

## Phase 8：持久化托管与分布式

> DurableTask + A2A。

| 任务                  | 需求 | 内容                 |
| --------------------- | ---- | -------------------- |
| DurableTask Console   | 4.1  | 内容流水线持久化版本 |
| DurableTask Functions | 4.2  | Serverless 部署      |
| A2A                   | 4.3  | 跨进程 Agent 通信    |

**验收**：服务重启后 Workflow 可恢复，Agent 可跨进程通信。

---

## 依赖关系

```
P1 (基础设施)
├── P2 (Agent 核心) ── P5 (Agent 扩展)
│   └── P3 (Workflow 核心)
│       ├── P6 (Workflow 高级)
│       └── P8 (托管/A2A)
├── P4 (Agent 智能)
└── P7 (安全/调试)
```

P1 → 2 → 3 为主线（最快出成果），P4/5/6 可部分并行，P7/8 收尾。
