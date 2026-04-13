# 聊天记录持久化 & 向量存储 -- 实施计划

> 本计划对应设计文档：[聊天记录持久化](../design/chat-history-persistence.md) + [向量存储](../design/vector-store.md)

---

## Sprint 1：DB 实体 + 接口扩展

**目标**：数据层就绪，不改变运行时行为。

| #   | 任务                                                  | 涉及文件                                | 验证方式                    |
| --- | ----------------------------------------------------- | --------------------------------------- | --------------------------- |
| 1.1 | 新增 `ChatSessionEntity`                              | `Entities/ChatSessionEntity.cs`         | 编译通过                    |
| 1.2 | 新增 `ChatMessageEntity`                              | `Entities/ChatMessageEntity.cs`         | 编译通过                    |
| 1.3 | `InkwellDbContext` 添加 `DbSet` + 索引                | `InkwellDbContext.cs`                   | EF Core InMemory 初始化成功 |
| 1.4 | 新增 `SessionInfo` / `ChatMessageRecord` 数据模型     | `ISessionPersistenceProvider.cs` 同文件 | 编译通过                    |
| 1.5 | 扩展 `ISessionPersistenceProvider` 接口（5 个新方法） | `ISessionPersistenceProvider.cs`        | 编译通过                    |
| 1.6 | `InMemorySessionPersistenceProvider` 实现新增方法     | `InMemorySessionPersistenceProvider.cs` | 单元测试验证 CRUD           |

**不涉及**：运行时调用链、前端、AGUI 端点。

---

## Sprint 2：EF Core 持久化实现

**目标**：可切换到数据库存储。

| #   | 任务                                                    | 涉及文件                                         | 验证方式                        |
| --- | ------------------------------------------------------- | ------------------------------------------------ | ------------------------------- |
| 2.1 | 实现 `EfCoreSessionPersistenceProvider`                 | `Providers/EfCoreSessionPersistenceProvider.cs`  | 单元测试                        |
| 2.2 | 注册扩展方法 `UseEfCoreSessionPersistence`              | `InkwellServiceCollectionExtensions.cs` 或新文件 | DI 切换验证                     |
| 2.3 | SqlServer Migration 添加 ChatSessions / ChatMessages 表 | `Migrations/`                                    | `dotnet ef migrations add` 成功 |
| 2.4 | `Program.cs` 按环境切换（InMemory 开发 / EF Core 生产） | `Program.cs`                                     | 启动验证                        |

---

## Sprint 3：AGUI Session 管理

**目标**：会话跨请求连续，对话历史自动恢复。这是核心关键变更。

| #   | 任务                                               | 涉及文件                             | 验证方式                  |
| --- | -------------------------------------------------- | ------------------------------------ | ------------------------- |
| 3.1 | 实现 `MapAGUIWithSession` 扩展方法                 | `InkwellAGUIExtensions.cs`（新文件） | 编译通过                  |
| 3.2 | 实现 `WrapWithPersistence` 流结束后持久化          | 同上                                 | 编译通过                  |
| 3.3 | 实现 `BuildRunOptions` 请求参数构建                | 同上                                 | 编译通过                  |
| 3.4 | 添加 `builder.Services.AddAGUI()` 注册             | `Program.cs`                         | 启动无错                  |
| 3.5 | 替换所有 `app.MapAGUI` -> `app.MapAGUIWithSession` | `Program.cs`                         | Agent / Workflow 端点正常 |
| 3.6 | 验证：发消息 -> 刷新页面 -> 重新发消息能延续上下文 | 手动测试                             | Agent 记得前面的对话      |

**依赖**：Sprint 1（接口 + InMemory 实现）。

---

## Sprint 4：Agent 统一配置 ChatHistoryProvider

**目标**：所有 Agent 都有短期对话记忆能力。

| #   | 任务                                                                               | 涉及文件                       | 验证方式                       |
| --- | ---------------------------------------------------------------------------------- | ------------------------------ | ------------------------------ |
| 4.1 | 提取 `CreateDefaultChatHistoryProvider` 通用工厂                                   | `InkwellAgents.cs`             | 编译通过                       |
| 4.2 | Critic / MarketAnalyst / CompetitorAnalyst / SeoOptimizer 加上 ChatHistoryProvider | `InkwellAgents.cs`             | 各 Agent 多轮对话保持上下文    |
| 4.3 | Translator / ImageAnalyst 加上 ChatHistoryProvider                                 | `InkwellAgents.cs`             | 同上                           |
| 4.4 | Coordinator 加上 ChatHistoryProvider                                               | `InkwellAgents.cs`             | 同上                           |
| 4.5 | `DeclarativeAgentLoader` 加上 ChatHistoryProvider                                  | `DeclarativeAgentLoader.cs`    | 声明式 Agent 多轮可用          |
| 4.6 | `DeclarativeWorkflowLoader` 加上 ChatHistoryProvider                               | `DeclarativeWorkflowLoader.cs` | 声明式 Workflow Agent 多轮可用 |

**依赖**：Sprint 3（session 管理就位后才有意义）。

---

## Sprint 5：会话管理 API

**目标**：前端可以查询、管理历史会话。

| #   | 任务                                                                        | 涉及文件                            | 验证方式                         |
| --- | --------------------------------------------------------------------------- | ----------------------------------- | -------------------------------- |
| 5.1 | 新增 `SessionsController`（6 个端点）                                       | `Controllers/SessionsController.cs` | Swagger / curl 测试              |
| 5.2 | GET `/api/sessions?agentId={id}` -- 会话列表                                | 同上                                | 返回 SessionInfo[]               |
| 5.3 | GET `/api/sessions/{threadId}` -- 会话详情                                  | 同上                                | 返回 SessionInfo                 |
| 5.4 | GET `/api/sessions/{threadId}/messages` -- 消息列表                         | 同上                                | 返回 ChatMessageRecord[]         |
| 5.5 | POST `/api/sessions` -- 创建会话                                            | 同上                                | 返回新 SessionInfo               |
| 5.6 | PATCH `/api/sessions/{threadId}` -- 更新标题                                | 同上                                | 标题已更新                       |
| 5.7 | DELETE `/api/sessions/{threadId}` -- 删除会话                               | 同上                                | 204 + 数据已删除                 |
| 5.8 | MapAGUIWithSession 中实现消息写入（用户 + Agent 回复 -> ChatMessageEntity） | `InkwellAGUIExtensions.cs`          | 对话后 messages 端点返回本轮消息 |
| 5.9 | 首条消息自动生成会话标题（截取前 50 字符）                                  | `InkwellAGUIExtensions.cs`          | 会话列表显示标题                 |

**依赖**：Sprint 3。

---

## Sprint 6：前端会话管理

**目标**：前端集成会话侧栏，支持历史会话切换和恢复。

| #   | 任务                                                         | 涉及文件                         | 验证方式                            |
| --- | ------------------------------------------------------------ | -------------------------------- | ----------------------------------- |
| 6.1 | `useAGUIAgent` 改造：只发当前用户消息 + threadId             | `use-agui-agent.ts`              | Network 面板确认请求体只有 1 条消息 |
| 6.2 | `useAGUIAgent` 新增 `loadMessages` 方法：调 API 恢复消息列表 | `use-agui-agent.ts`              | 切换会话后消息列表恢复              |
| 6.3 | 新增 `useSessionList` Hook                                   | `hooks/use-session-list.ts`      | 加载会话列表                        |
| 6.4 | 新增会话列表侧栏组件                                         | `components/session-sidebar.tsx` | UI 展示                             |
| 6.5 | 对话页面集成侧栏（左右分栏）                                 | `pipeline-run-page.tsx`          | 侧栏 + 对话区                       |
| 6.6 | 侧栏功能：创建新会话                                         | `session-sidebar.tsx`            | 点击后清空对话 + 新 threadId        |
| 6.7 | 侧栏功能：切换会话                                           | `session-sidebar.tsx`            | 点击后加载历史消息                  |
| 6.8 | 侧栏功能：删除会话                                           | `session-sidebar.tsx`            | 删除后从列表移除                    |
| 6.9 | 侧栏功能：重命名会话                                         | `session-sidebar.tsx`            | 编辑后标题更新                      |

**依赖**：Sprint 5（API 就绪）。

---

## Sprint 7：聊天裁剪升级

**目标**：对话型 Agent 使用 Pipeline 裁剪策略，避免 context window 溢出。

| #   | 任务                                            | 涉及文件           | 验证方式               |
| --- | ----------------------------------------------- | ------------------ | ---------------------- |
| 7.1 | Writer 升级为 `PipelineCompactionStrategy`      | `InkwellAgents.cs` | 长对话不溢出           |
| 7.2 | Coordinator 升级为 `PipelineCompactionStrategy` | `InkwellAgents.cs` | 同上                   |
| 7.3 | 验证：30+ 轮对话后 Agent 仍正常回复             | 手动测试           | 不报 token 超限        |
| 7.4 | 验证：摘要保留关键信息                          | 手动测试           | 问"之前聊过什么"能回答 |

**依赖**：Sprint 4（Agent 统一配 ChatHistoryProvider 后才能升级）。

---

## Sprint 8：向量存储 + 长期记忆

**目标**：跨 session 的语义检索记忆能力。

| #   | 任务                                                         | 涉及文件                                              | 验证方式                  |
| --- | ------------------------------------------------------------ | ----------------------------------------------------- | ------------------------- |
| 8.1 | `AzureOpenAIOptions` 新增 `Embedding` 配置段                 | `AzureOpenAIOptions.cs`                               | 配置加载成功              |
| 8.2 | 实现 `UseAzureOpenAIEmbedding` 注册方法                      | `AzureOpenAIServiceCollectionExtensions.cs`           | DI 解析成功               |
| 8.3 | 实现 `UseInMemoryVectorStore` 注册方法                       | `VectorStoreServiceCollectionExtensions.cs`（新文件） | DI 解析成功               |
| 8.4 | `Program.cs` 接入 Embedding + VectorStore                    | `Program.cs`                                          | 启动无错                  |
| 8.5 | Writer Agent 挂载 `ChatHistoryMemoryProvider`                | `InkwellAgents.cs`                                    | 跨 session 记忆生效       |
| 8.6 | Coordinator Agent 挂载 `ChatHistoryMemoryProvider`           | `InkwellAgents.cs`                                    | 同上                      |
| 8.7 | 验证：Session A 聊过"量子计算" -> Session B 问"之前写过什么" | 手动测试                                              | 能检索到 Session A 的内容 |
| 8.8 | appsettings.json 添加 Embedding 模型配置                     | `appsettings.json` / `appsettings.Development.json`   | 配置完整                  |

**依赖**：Sprint 4 + Embedding 模型部署就绪。

---

## Sprint 9：体验优化

**目标**：打磨细节。

| #   | 任务                                             | 涉及文件                       | 验证方式            |
| --- | ------------------------------------------------ | ------------------------------ | ------------------- |
| 9.1 | 会话标题自动生成（调用 LLM 总结首轮对话）        | `InkwellAGUIExtensions.cs`     | 标题是有意义的摘要  |
| 9.2 | 会话搜索（按标题模糊搜索）                       | `SessionsController.cs` + 前端 | 搜索能过滤          |
| 9.3 | 会话导出（Markdown）                             | `SessionsController.cs`        | 下载 .md 文件       |
| 9.4 | 过期会话自动清理                                 | 后台定时任务                   | 超过 N 天的自动删除 |
| 9.5 | 按需引入生产向量连接器（Qdrant / AzureAISearch） | NuGet + 注册方法               | 切换后正常工作      |

**依赖**：Sprint 6 + 8。

---

## 依赖关系

```
Sprint 1 (DB 实体 + 接口)
    |
    +-- Sprint 2 (EF Core 实现)    <-- 可与 Sprint 3 并行
    |
    +-- Sprint 3 (AGUI Session 管理)  <-- 核心关键路径
            |
            +-- Sprint 4 (Agent 统一配 ChatHistoryProvider)
            |       |
            |       +-- Sprint 7 (聊天裁剪升级)
            |       |
            |       +-- Sprint 8 (向量存储 + 长期记忆)
            |
            +-- Sprint 5 (会话管理 API)
                    |
                    +-- Sprint 6 (前端会话管理)
                            |
                            +-- Sprint 9 (体验优化)
```

---

## 里程碑

| 里程碑             | 完成 Sprint | 用户可感知效果                           |
| ------------------ | ----------- | ---------------------------------------- |
| **M1：对话不丢失** | 1 + 3 + 4   | 刷新页面后对话不丢失，Agent 记得上下文   |
| **M2：会话管理**   | 2 + 5 + 6   | 侧栏展示历史会话，可切换/删除/重命名     |
| **M3：智能裁剪**   | 7           | 30+ 轮长对话不溢出，自动摘要保留关键信息 |
| **M4：长期记忆**   | 8           | 跨会话语义检索历史对话                   |
| **M5：体验完善**   | 9           | 会话搜索/导出/自动清理                   |
