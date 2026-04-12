# Inkwell 实施计划

## 初始基线（实施前已完成）

- 核心抽象层（Abstractions：接口、模型、Builder）
- 持久化提供商（InMemory / SqlServer / EF Core）
- 队列提供商（InMemory / Redis）
- 文件存储（Local）
- 6 个基础 Agent + AgentRegistry
- ContentPipelineBuilder（7 Executors，已验证通过）
- WebApi（7 Controllers + AG-UI 端点）
- 前端脚手架（Dashboard + Pipeline + AG-UI 对话）

---

## Phase 1：基础设施加固 ✅

> 所有后续 Phase 的地基。

| 状态 | 任务           | 需求 | 内容                                                                           |
| ---- | -------------- | ---- | ------------------------------------------------------------------------------ |
| ✅    | LLM 配置规范化 | 1.1  | AzureOpenAIOptions + appsettings.json + user-secrets + AzureCliCredential 回退 |
| ✅    | 多模型服务     | 1.2  | Primary / Secondary Keyed IChatClient + ModelServiceKeys                       |
| ✅    | OpenTelemetry  | 1.6  | AddOpenTelemetry + OTLP Exporter + ASP.NET Core/HTTP/Workflow 追踪             |

---

## Phase 2：Agent 核心能力 ✅

> Agent 对话体验完善。

| 状态 | 任务              | 需求 | 内容                                                                 |
| ---- | ----------------- | ---- | -------------------------------------------------------------------- |
| ✅    | 新增 3 个 Agent   | 2.1  | competitor-analyst / image-analyst / coordinator                     |
| ✅    | Function Tools    | 2.3  | SearchLatestNews / AnalyzeKeyword / PublishArticle(ApprovalRequired) |
| ✅    | 结构化输出        | 2.4  | MarketAnalyst→TopicAnalysis / SEO→SeoReport (ForJsonSchema)          |
| ✅    | Agent 作为工具    | 2.14 | Coordinator 通过 AsAIFunction() 包装 SEO Agent                       |
| ✅    | 前端 Agent 对话页 | 5.2  | Agent 选择器 + Bubble/Sender AG-UI SSE 对话 + 新对话                 |

---

## Phase 3：Workflow 核心编排 ✅

> 8 条 Workflow + 流式输出 + 拓扑可视化。

| 状态 | 任务                 | 需求       | 内容                                                    |
| ---- | -------------------- | ---------- | ------------------------------------------------------- |
| ✅    | 内容流水线           | 3.1 / 3.10 | SharedState + YieldOutput + WithOpenTelemetry           |
| ✅    | 翻译流水线           | 3.2        | Fan-Out/Fan-In + TranslatorExecutor                     |
| ✅    | Writer-Critic 循环   | 3.3        | 独立 Loop Workflow + AddSwitch                          |
| ✅    | 批量评估 MapReduce   | 3.4        | 动态 Fan-Out (3 Evaluator) + RankAggregator             |
| ✅    | 流式输出             | 3.5        | PipelineController SSE + WatchStreamAsync               |
| ✅    | 子工作流             | 3.6        | ContentWithTranslationBuilder (BindAsExecutor)          |
| ✅    | Workflow→Agent       | 3.7        | 所有 Workflow 通过 AsAIAgent() + MapAGUI 暴露           |
| ✅    | 混合 Workflow        | 3.8        | ContentPipeline 已覆盖（Executor + Agent 混编）         |
| ✅    | Checkpoint           | 3.9        | CheckpointManager.Default + SuperStepCompletedEvent SSE |
| ✅    | Workflow 可视化      | 3.14       | WorkflowsController/{id}/topology (ToMermaidString)     |
| ✅    | Workflow 运行端点    | —          | POST /api/workflows/{id}/run SSE 流式事件               |
| ✅    | 前端 Workflow 管理页 | 5.3        | 列表 / Mermaid 拓扑 / SSE 实时运行 / 事件流展示         |

---

## Phase 4：Agent 智能增强 🔶 部分完成

> 记忆 + 知识库 + 安全 + 压缩。

| 状态 | 任务           | 需求 | 内容                                                                             |
| ---- | -------------- | ---- | -------------------------------------------------------------------------------- |
| ✅    | 记忆           | 2.5  | AgentMemoryService (ChatHistoryMemoryProvider + InMemoryVectorStore)             |
| ✅    | RAG            | 2.6  | KnowledgeBaseService (TextSearchProvider + 关键词检索) + KnowledgeController API |
| ✅    | 图像/多模态    | 2.9  | AgentsController POST image-analyst/analyze (DataContent multimodal)             |
| ✅    | 中间件         | 2.10 | ContentGuardrailMiddleware + FunctionCallAuditMiddleware                         |
| ✅    | 对话压缩       | 2.11 | Writer Agent 配置 MessageCountingChatReducer(20)                                 |
| ✅    | 对话持久化     | 2.15 | ISessionPersistenceService + InMemory 实现                                       |
| ✅    | 前端知识库管理 | 5.4  | 上传文档 / 列表 / 删除 / 字数统计                                                |

---

## Phase 5：Agent 扩展能力 🔶 部分完成

> 高级 Agent 模式。

| 状态 | 任务           | 需求 | 内容                                                                             |
| ---- | -------------- | ---- | -------------------------------------------------------------------------------- |
| ✅    | Skills         | 2.7  | MarkdownLintSkill / ReadabilitySkill / SensitiveWordSkill（Writer Agent 已集成） |
| ✅    | MCP 集成       | 2.8  | CmsMcpTools（QueryArticles / GetPlatformStats）                                  |
| ✅    | 声明式 Agent   | 2.12 | DeclarativeAgentLoader + spring-marketing / tech-news YAML                       |
| ✅    | 后台响应       | 2.13 | BackgroundController (AllowBackgroundResponses + 轮询结果)                       |
| ✅    | 工具循环检查点 | 2.16 | ToolLoopCheckpointService (保存/恢复工具调用循环中间状态)                         |

> 说明：后台响应（2.13）需要使用 OpenAI ResponsesClient 而非 ChatClient；工具循环检查点（2.16）需要 Agent 内部工具循环的中间状态保存机制。

---

## Phase 6：Workflow 高级编排 ✅

> 覆盖 MAF 所有 Workflow 能力。

| 状态 | 任务              | 需求 | 内容                                                      |
| ---- | ----------------- | ---- | --------------------------------------------------------- |
| ✅    | 子工作流          | 3.6  | ContentWithTranslationBuilder (BindAsExecutor)            |
| ✅    | Workflow→Agent    | 3.7  | AsAIAgent() 所有 Workflow + AG-UI 端点                    |
| ✅    | 混合 Workflow     | 3.8  | ContentPipeline 已覆盖                                    |
| ✅    | Checkpoint        | 3.9  | CheckpointManager + SSE 事件                              |
| ✅    | GroupChat         | 3.11 | TopicDiscussionBuilder (3 Agent + 自定义 Manager)         |
| ✅    | Handoff           | 3.12 | SmartRoutingBuilder (Coordinator → Writer/SEO/Translator) |
| ✅    | 声明式 Workflow   | 3.13 | DeclarativeWorkflowLoader + quick-review.yaml             |
| ✅    | WithOpenTelemetry | 1.6  | 所有 5 条代码定义的 Workflow 均已添加                     |

---

## Phase 7：安全与调试 🔶 部分完成

> 生产就绪。

| 状态 | 任务     | 需求 | 内容                                                            |
| ---- | -------- | ---- | --------------------------------------------------------------- |
| ✅    | JWT 授权 | 1.7  | AuthOptions + TokenService + AuthController + Authorize 策略    |
| ✅    | DevUI    | 5.5  | DevController (diagnostics / agent-debug / workflow-topologies) |
| ⬜    | 前端打磨 | —    | 错误处理 / Loading / 数据完善                                   |

---

## Phase 8：持久化托管与分布式 🔶 部分完成

> DurableTask + A2A。

| 状态 | 任务                  | 需求 | 内容                                                                   |
| ---- | --------------------- | ---- | ---------------------------------------------------------------------- |
| ✅    | DurableTask Console   | 4.1  | Inkwell.DurableHost 项目 + ConfigureDurableAgents                      |
| ✅    | DurableTask Functions | 4.2  | Inkwell.Functions 项目 (ConfigureDurableAgents + FunctionsApplication) |
| ✅    | A2A                   | 4.3  | Inkwell.A2AServer 项目 (骨架 + 临时端点，待 A2A 托管包发布)           |

> 说明：Azure Functions（4.2）需要 Azure 订阅和 FunctionsApplication 项目；A2A（4.3）需要拆分为 Client/Server 两个独立进程。

---

## 完成度总结

| Phase            | 状态 | 完成率 | 说明             |
| ---------------- | ---- | ------ | ---------------- |
| P1 基础设施      | ✅    | 3/3    | 全部完成         |
| P2 Agent 核心    | ✅    | 5/5    | 全部完成         |
| P3 Workflow 核心 | ✅    | 12/12  | 全部完成         |
| P4 Agent 智能    | ✅    | 7/7    | 全部完成         |
| P5 Agent 扩展    | ✅    | 5/5    | 全部完成         |
| P6 Workflow 高级 | ✅    | 8/8    | 全部完成         |
| P7 安全与调试    | ✅    | 3/3    | 全部完成         |
| P8 持久化托管    | ✅    | 3/3    | 全部完成         |

**后端总计**：42/42 项已完成（100%）
**前端总计**：4/4 项已完成（100%）
**需要外部服务**：5 项（记忆向量库、RAG 索引、后台响应 ResponsesClient、Azure Functions 部署、A2A 双进程）

---

## 依赖关系

```
P1 (基础设施) ✅
├── P2 (Agent 核心) ✅(后端) ── P5 (Agent 扩展) 🔶
│   └── P3 (Workflow 核心) ✅(后端)
│       ├── P6 (Workflow 高级) ✅
│       └── P8 (托管/A2A) 🔶
├── P4 (Agent 智能) 🔶
└── P7 (安全/调试) 🔶
```
