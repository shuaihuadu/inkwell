# Inkwell 聊天记录持久化设计方案

## 1. 架构概览

### 1.1 数据流

```
┌───────────────────────────────────────────────────────────────────────┐
│                            Frontend                                   │
│                                                                       │
│  ┌────────────────┐  ┌──────────────────┐  ┌──────────────────────┐  │
│  │ 会话列表侧栏    │  │ useAGUIAgent     │  │ useSessionList       │  │
│  │ GET /sessions   │  │ threadId + 当前  │  │ 创建/删除/重命名     │  │
│  │                │  │ 用户消息          │  │                      │  │
│  └───────┬────────┘  └───────┬──────────┘  └──────────┬───────────┘  │
└──────────┼───────────────────┼─────────────────────────┼─────────────┘
           │                   │                         │
           ▼                   ▼                         ▼
┌───────────────────────────────────────────────────────────────────────┐
│                         ASP.NET Core                                  │
│                                                                       │
│  SessionsController      MapAGUIWithSession         SessionsController│
│  (查询/管理)             (AGUI + Session 生命周期)   (CRUD)           │
│                                │                                      │
│                     ┌──────────┴──────────┐                           │
│                     │ 1. Load Session     │                           │
│                     │ 2. RunStreamingAsync │                           │
│                     │ 3. Save Session     │                           │
│                     │ 4. Save Messages    │                           │
│                     └──────────┬──────────┘                           │
│                                │                                      │
│                                ▼                                      │
│            ISessionPersistenceProvider                                 │
│            ┌──────────────┬────────────────┐                          │
│            │ InMemory     │ EF Core        │                          │
│            │ (开发环境)    │ (生产环境)      │                          │
│            └──────────────┴────────────────┘                          │
└───────────────────────────────┬───────────────────────────────────────┘
                                │
                                ▼
┌───────────────────────────────────────────────────────────────────────┐
│                         数据库 (EF Core)                              │
│                                                                       │
│  ChatSessions (1) ────── (N) ChatMessages                             │
│  ┌──────────────────┐       ┌─────────────────────┐                   │
│  │ Id = threadId    │       │ Id                  │                   │
│  │ AgentId          │       │ SessionId (FK)      │                   │
│  │ Title            │       │ Role                │                   │
│  │ SessionState     │       │ Content             │                   │
│  │ MessageCount     │       │ Status              │                   │
│  │ CreatedAt        │       │ CreatedAt           │                   │
│  │ UpdatedAt        │       │                     │                   │
│  └──────────────────┘       └─────────────────────┘                   │
└───────────────────────────────────────────────────────────────────────┘
```

### 1.2 核心设计原则

- **复用 MAF 机制**：通过 `AgentSession.StateBag` + `SerializeSessionAsync` / `DeserializeSessionAsync` 持久化，`InMemoryChatHistoryProvider` 自动随 session 序列化/恢复
- **双写存储**：`SessionState`（MAF 序列化 JSON）保证 Agent 运行恢复；`ChatMessageEntity` 是展平消息，供前端 UI 展示
- **前端最小化**：前端只发 `threadId` + 当前用户消息，不再重放全部历史

---

## 2. 数据库设计

### 2.1 ChatSessionEntity

```csharp
[Table("ChatSessions")]
public sealed class ChatSessionEntity
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;          // = threadId

    [Required]
    [MaxLength(100)]
    public string AgentId { get; set; } = string.Empty;      // writer / critic / workflow-xxx

    [MaxLength(200)]
    public string? Title { get; set; }                        // 自动从首条用户消息截取

    [Required]
    public string SessionState { get; set; } = string.Empty;  // SerializeSessionAsync() 的 JSON

    public int MessageCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
```

`SessionState` 存储 `agent.SerializeSessionAsync(session)` 的完整 JSON，包含 `StateBag` 中 `InMemoryChatHistoryProvider.State.Messages`。Agent 运行时只依赖此字段恢复状态。

### 2.2 ChatMessageEntity

```csharp
[Table("ChatMessages")]
public sealed class ChatMessageEntity
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;   // FK -> ChatSessionEntity.Id

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;        // user / assistant / system

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "done";            // done / error

    public DateTimeOffset CreatedAt { get; set; }
}
```

仅用于前端展示（恢复显示、搜索、会话预览），不参与 Agent 运行。

### 2.3 索引

```csharp
modelBuilder.Entity<ChatSessionEntity>(entity =>
{
    entity.HasIndex(e => e.AgentId);
    entity.HasIndex(e => e.UpdatedAt);
});

modelBuilder.Entity<ChatMessageEntity>(entity =>
{
    entity.HasIndex(e => e.SessionId);
    entity.HasIndex(e => e.CreatedAt);
});
```

---

## 3. 后端服务接口

### 3.1 ISessionPersistenceProvider 扩展

在现有接口基础上新增消息和元数据查询方法：

```csharp
public interface ISessionPersistenceProvider
{
    // 现有方法
    Task SaveSessionAsync(string sessionId, string agentId, JsonElement sessionState, CancellationToken ct);
    Task<JsonElement?> LoadSessionAsync(string sessionId, CancellationToken ct);
    Task<IReadOnlyList<string>> ListSessionsAsync(string agentId, CancellationToken ct);
    Task DeleteSessionAsync(string sessionId, CancellationToken ct);

    // 新增方法
    Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken ct);
    Task<IReadOnlyList<SessionInfo>> ListSessionInfosAsync(string agentId, CancellationToken ct);
    Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken ct);
    Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessageRecord> messages, CancellationToken ct);
    Task<IReadOnlyList<ChatMessageRecord>> GetMessagesAsync(string sessionId, CancellationToken ct);
}
```

数据模型：

```csharp
public sealed record SessionInfo(
    string Id, string AgentId, string? Title,
    int MessageCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record ChatMessageRecord(
    string Id, string Role, string Content, string Status, DateTimeOffset CreatedAt);
```

### 3.2 EfCoreSessionPersistenceProvider

核心实现逻辑：

| 方法                    | 实现策略                                                                   |
| ----------------------- | -------------------------------------------------------------------------- |
| `SaveSessionAsync`      | Upsert：`FindAsync` -> 存在则更新 `SessionState`/`UpdatedAt`，不存在则 Add |
| `LoadSessionAsync`      | `FindAsync` -> `JsonDocument.Parse(entity.SessionState).RootElement`       |
| `ListSessionInfosAsync` | 按 `AgentId` 过滤，`OrderByDescending(UpdatedAt)`                          |
| `SaveMessagesAsync`     | `AddRangeAsync` 追加 `ChatMessageEntity` + 更新 `MessageCount`             |
| `GetMessagesAsync`      | 按 `SessionId` 过滤，`OrderBy(CreatedAt)`                                  |
| `DeleteSessionAsync`    | 级联删除 Session + Messages                                                |

### 3.3 InMemorySessionPersistenceProvider 适配

同步扩展现有实现，在内存字典中增加消息存储，保持开发环境可用。

---

## 4. AGUI Session 管理

### 4.1 MapAGUIWithSession

由于 MAF 的 `MapAGUI` 不支持传入 session，封装自定义注册方法管理 session 生命周期：

```csharp
public static class InkwellAGUIExtensions
{
    public static IEndpointConventionBuilder MapAGUIWithSession(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        AIAgent aiAgent,
        string agentId)
    {
        return endpoints.MapPost(pattern, async (
            [FromBody] RunAgentInput? input,
            HttpContext context,
            ISessionPersistenceProvider sessionStore,
            IOptions<JsonOptions> jsonOptions,
            ILogger<AGUIServerSentEventsResult> sseLogger,
            CancellationToken ct) =>
        {
            if (input is null) return Results.BadRequest();

            string threadId = input.ThreadId;
            var jso = jsonOptions.Value.SerializerOptions;

            // 1) 加载或创建 session
            JsonElement? saved = await sessionStore.LoadSessionAsync(threadId, ct);
            AgentSession session = saved.HasValue
                ? await aiAgent.DeserializeSessionAsync(saved.Value)
                : await aiAgent.CreateSessionAsync(ct);

            // 2) 构建消息和运行选项
            var messages = input.Messages.AsChatMessages(jso);
            var clientTools = input.Tools?.AsAITools().ToList();
            var runOptions = BuildRunOptions(input, clientTools);

            // 3) 流式执行（传入 session）
            var events = aiAgent.RunStreamingAsync(messages, session, runOptions, ct)
                .AsChatResponseUpdatesAsync()
                .FilterServerToolsFromMixedToolInvocationsAsync(clientTools, ct)
                .AsAGUIEventStreamAsync(threadId, input.RunId, jso, ct);

            // 4) 流结束后持久化
            var persistingEvents = WrapWithPersistence(events, async () =>
            {
                JsonElement state = await aiAgent.SerializeSessionAsync(session);
                await sessionStore.SaveSessionAsync(threadId, agentId, state, ct);
            });

            return new AGUIServerSentEventsResult(persistingEvents, sseLogger);
        });
    }

    private static async IAsyncEnumerable<BaseEvent> WrapWithPersistence(
        IAsyncEnumerable<BaseEvent> events,
        Func<Task> onComplete,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var evt in events.WithCancellation(ct))
        {
            yield return evt;
        }
        await onComplete();
    }

    private static ChatClientAgentRunOptions BuildRunOptions(
        RunAgentInput input, List<AITool>? clientTools)
    {
        return new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                Tools = clientTools,
                AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    ["ag_ui_state"] = input.State,
                    ["ag_ui_context"] = input.Context?
                        .Select(c => new KeyValuePair<string, string>(c.Description, c.Value))
                        .ToArray(),
                    ["ag_ui_forwarded_properties"] = input.ForwardedProperties,
                    ["ag_ui_thread_id"] = input.ThreadId,
                    ["ag_ui_run_id"] = input.RunId
                }
            }
        };
    }
}
```

### 4.2 Program.cs 替换

```csharp
// 替换前
app.MapAGUI(registration.AguiRoute, registration.Agent);

// 替换后
app.MapAGUIWithSession(registration.AguiRoute, registration.Agent, registration.Id);
```

### 4.3 消息保存时机

| 时机           | 保存内容                                     | 方式                                                 |
| -------------- | -------------------------------------------- | ---------------------------------------------------- |
| AGUI 流结束后  | `SessionState` -> `ChatSessionEntity`        | `SerializeSessionAsync` 后 upsert                    |
| AGUI 流结束后  | 用户消息 + Agent 回复 -> `ChatMessageEntity` | 从 session 的 `ChatHistoryProvider` 提取最新消息写入 |
| 会话首条消息时 | 自动生成 `Title`                             | 截取首条用户消息前 50 字符                           |

---

## 5. Agent 统一配置 ChatHistoryProvider

### 5.1 通用工厂

```csharp
private static InMemoryChatHistoryProvider CreateDefaultChatHistoryProvider(int retainCount = 20)
{
    return new InMemoryChatHistoryProvider(new()
    {
        ChatReducer = new MessageCountingChatReducer(retainCount)
    });
}
```

### 5.2 各 Agent 保留策略

| Agent                             | 保留消息数 | 说明                 |
| --------------------------------- | ---------- | -------------------- |
| Writer                            | 20         | 需要完整写作上下文   |
| Critic                            | 10         | 审核关注当前内容     |
| Translator                        | 6          | 翻译任务相对独立     |
| MarketAnalyst / CompetitorAnalyst | 10         | 分析需要一定上下文   |
| SeoOptimizer                      | 10         | SEO 优化需参考前文   |
| Coordinator                       | 20         | 调度需要理解完整对话 |
| ImageAnalyst                      | 6          | 图片分析相对独立     |
| 声明式 Agent                      | 10         | 默认保留量           |

### 5.3 声明式 Agent 适配

```csharp
AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = definition.Name,
    ChatOptions = new() { Instructions = definition.Instructions },
    ChatHistoryProvider = CreateDefaultChatHistoryProvider(10)
});
```

---

## 6. REST API 设计

### 6.1 SessionsController

| 方法   | 路径                                | 功能         | 响应                  |
| ------ | ----------------------------------- | ------------ | --------------------- |
| GET    | `/api/sessions?agentId={id}`        | 获取会话列表 | `SessionInfo[]`       |
| GET    | `/api/sessions/{threadId}`          | 获取会话详情 | `SessionInfo`         |
| GET    | `/api/sessions/{threadId}/messages` | 获取消息列表 | `ChatMessageRecord[]` |
| POST   | `/api/sessions`                     | 创建新会话   | `SessionInfo`         |
| PATCH  | `/api/sessions/{threadId}`          | 更新标题     | `SessionInfo`         |
| DELETE | `/api/sessions/{threadId}`          | 删除会话     | 204                   |

### 6.2 响应示例

```json
// GET /api/sessions?agentId=writer
[
  {
    "id": "cf843717-6f70-40c6-936e-eab5fbaa0642",
    "agentId": "writer",
    "title": "AI 在医疗健康领域的未来",
    "messageCount": 6,
    "createdAt": "2026-04-13T10:30:00Z",
    "updatedAt": "2026-04-13T10:35:00Z"
  }
]

// GET /api/sessions/{threadId}/messages
[
  {
    "id": "msg-001",
    "role": "user",
    "content": "写一篇关于 AI 在医疗健康领域的文章",
    "status": "done",
    "createdAt": "2026-04-13T10:30:00Z"
  },
  {
    "id": "msg-002",
    "role": "assistant",
    "content": "# AI 在医疗健康领域的未来 ...",
    "status": "done",
    "createdAt": "2026-04-13T10:30:15Z"
  }
]
```

---

## 7. 前端设计

### 7.1 useAGUIAgent 改造

发请求时只发当前用户消息 + threadId，不再重放全部历史：

```typescript
const allMessages: AGUIMessage[] = [
  { id: userMsg.id, role: "user", content: normalized },
];
```

页面加载或切换会话时调 `GET /api/sessions/{threadId}/messages` 恢复消息列表。

### 7.2 useSessionList Hook

```typescript
interface UseSessionListReturn {
  sessions: SessionInfo[];
  loading: boolean;
  activeSessionId: string | null;
  createSession: (agentId: string) => Promise<string>;
  selectSession: (sessionId: string) => void;
  deleteSession: (sessionId: string) => void;
  renameSession: (sessionId: string, title: string) => void;
}
```

### 7.3 对话页面布局

```
┌──────────────────┬──────────────────────────────────────────┐
│ 历史会话          │ 对话  [Agent ?]                [新对话]  │
│                  │                                          │
│ > AI 医疗健康    │  用户: 写一篇关于 AI 在医疗健康领域的文章│
│   内容优化策略    │                                          │
│   SEO 分析报告    │  助手: # AI 在医疗健康领域的未来         │
│                  │        AI 正在迅速改变...                 │
│                  │                                          │
│                  │  用户: 继续深入写第二部分                 │
│                  │                                          │
│                  │  助手: ## 深度融合与创新应用...           │
│                  │                                          │
│                  │  ┌──────────────────────────────────┐    │
│                  │  │ 输入消息...                   [>] │    │
│                  │  └──────────────────────────────────┘    │
└──────────────────┴──────────────────────────────────────────┘
```

侧栏功能：
- 会话列表按 `updatedAt` 倒序
- 点击切换会话，加载历史消息
- 右键菜单：重命名、删除
- "新对话"按钮创建新 session

---

## 8. 聊天裁剪（Chat Compaction）

### 8.1 问题

随着对话轮次增加，发送给 LLM 的消息列表会超出模型的 context window 限制，同时产生不必要的 token 消耗。需要在保留对话连贯性的同时控制消息总量。

### 8.2 MAF 裁剪能力

MAF 提供了两层裁剪机制：

**第一层：IChatReducer（轻量）**

`InMemoryChatHistoryProvider` 内置支持 `IChatReducer`，在消息读取前或写入后自动触发：

```csharp
new InMemoryChatHistoryProvider(new()
{
    ChatReducer = new MessageCountingChatReducer(20),
    ReducerTriggerEvent = ChatReducerTriggerEvent.BeforeMessagesRetrieval
})
```

`MessageCountingChatReducer` 来自 `Microsoft.Extensions.AI`，按消息条数截断。

**第二层：CompactionStrategy（高级）**

MAF 提供了 5 种内置压缩策略，可组成 Pipeline：

| 策略                              | 作用                         | 触发条件        |
| --------------------------------- | ---------------------------- | --------------- |
| `TruncationCompactionStrategy`    | 按 token 数截断最早的消息    | 超出 token 上限 |
| `SummarizationCompactionStrategy` | 调用 LLM 将旧消息摘要为一条  | 超出 token 上限 |
| `SlidingWindowCompactionStrategy` | 保留最近 N 条，丢弃其余      | 超出消息数      |
| `ToolResultCompactionStrategy`    | 压缩工具调用结果（保留摘要） | 结果超长        |
| `PipelineCompactionStrategy`      | 串联多个策略按优先级执行     | 任意            |

Pipeline 模式示例（从温和到激进）：

```csharp
var compactionPipeline = new PipelineCompactionStrategy([
    new ToolResultCompactionStrategy(chatClient, maxToolResultTokens: 500),
    new SummarizationCompactionStrategy(chatClient, maxTokenCount: 4000),
    new SlidingWindowCompactionStrategy(maxMessages: 30),
    new TruncationCompactionStrategy(maxTokenCount: 8000)
]);
```

### 8.3 Inkwell 裁剪方案

采用分层策略，按 Agent 类型配置：

**对话型 Agent（Writer、Coordinator）：**

```csharp
ChatHistoryProvider = new InMemoryChatHistoryProvider(new()
{
    ChatReducer = new PipelineCompactionStrategy([
        new ToolResultCompactionStrategy(chatClient, maxToolResultTokens: 500),
        new SummarizationCompactionStrategy(chatClient, maxTokenCount: 6000),
        new TruncationCompactionStrategy(maxTokenCount: 12000)
    ]).AsChatReducer(),
    ReducerTriggerEvent = ChatReducerTriggerEvent.BeforeMessagesRetrieval
})
```

- 先压缩工具结果 -> 再摘要旧历史 -> 最后截断兜底
- 摘要使用同一 `IChatClient`，不需要额外模型

**任务型 Agent（Critic、Translator、Analyst）：**

```csharp
ChatHistoryProvider = new InMemoryChatHistoryProvider(new()
{
    ChatReducer = new MessageCountingChatReducer(retainCount),
    ReducerTriggerEvent = ChatReducerTriggerEvent.BeforeMessagesRetrieval
})
```

- 简单按消息条数截断，任务型 Agent 不需要复杂摘要

### 8.4 裁剪触发点

| 触发事件                  | 行为                                                |
| ------------------------- | --------------------------------------------------- |
| `BeforeMessagesRetrieval` | `InvokingAsync` 时裁剪，确保发给 LLM 的消息在限制内 |
| `AfterMessageAdded`       | `InvokedAsync` 时裁剪，确保持久化的消息不会无限膨胀 |

推荐使用 `BeforeMessagesRetrieval`，这样 `ChatMessageEntity`（前端展示用）保留完整历史，而发给 LLM 的 context 被裁剪。

---

## 9. 长期记忆（Long-Term Memory）

### 9.1 问题

裁剪只能保留最近的对话窗口，更早的对话信息被丢弃。对于持续使用的用户，Agent 无法记住跨会话的偏好、写作风格、历史主题等长期上下文。

### 9.2 MAF Memory 能力

MAF 提供了 `ChatHistoryMemoryProvider`，基于向量存储实现语义检索式长期记忆：

```
用户发送消息
    ↓
ChatHistoryMemoryProvider.InvokingAsync
    ↓ 向量搜索相关历史片段
    ↓ 注入到当前请求消息中
    ↓
LLM 生成回复（带有长期记忆上下文）
    ↓
ChatHistoryMemoryProvider.InvokedAsync
    ↓ 将本轮对话写入向量存储
    ↓
下次对话可以检索到本轮内容
```

`ChatHistoryMemoryProvider` 继承自 `MessageAIContextProvider`，支持两种检索模式：

| 模式                      | 配置 | 行为                              |
| ------------------------- | ---- | --------------------------------- |
| `BeforeAIInvoke`          | 默认 | 自动搜索并注入相关记忆到消息列表  |
| `OnDemandFunctionCalling` | 可选 | 注册搜索工具，让 LLM 决定何时检索 |

### 9.3 Inkwell 长期记忆方案

> 向量存储的抽象设计、连接器选型和 DI 注册方案见 [向量存储设计方案](vector-store.md)。

#### 9.3.1 架构

```
┌──────────────────────────────────────────────────────┐
│                     Agent 运行时                      │
│                                                      │
│  ChatHistoryProvider        ChatHistoryMemoryProvider │
│  (短期：最近 N 轮)          (长期：语义检索)          │
│         │                           │                │
│         ↓                           ↓                │
│  AgentSession.StateBag      VectorStore              │
│  (序列化到 DB)              (向量数据库)              │
└──────────────────────────────────────────────────────┘
```

短期记忆（`ChatHistoryProvider`）和长期记忆（`ChatHistoryMemoryProvider`）并行工作：
- 短期记忆：保留最近 N 轮完整对话，用于当前 session 的上下文连贯
- 长期记忆：将所有对话嵌入向量存储，跨 session 语义检索相关历史

#### 9.3.2 向量存储选型

| 环境 | 存储                                           | 说明               |
| ---- | ---------------------------------------------- | ------------------ |
| 开发 | `InMemoryVectorStore`                          | 零依赖，重启丢失   |
| 生产 | Azure AI Search / Qdrant / PostgreSQL pgvector | 可根据部署环境选择 |

#### 9.3.3 Embedding 服务

复用现有 Azure OpenAI 配置（`AzureOpenAIOptions.Embedding`），注册 `IEmbeddingGenerator`：

```csharp
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    new AzureOpenAIClient(new Uri(endpoint), credential)
        .GetEmbeddingClient(deploymentName)
        .AsIEmbeddingGenerator();
```

#### 9.3.4 记忆范围（Scope）

`ChatHistoryMemoryProvider` 支持 `StorageScope` 和 `SearchScope` 配置：

| Scope              | 用途                                                  |
| ------------------ | ----------------------------------------------------- |
| `UserId`           | 用户级记忆 — 同一用户跨 Agent、跨会话共享             |
| `AgentId`          | Agent 级记忆 — 同一 Agent 所有会话共享                |
| `SessionId`        | 会话级记忆 — 仅当前会话内检索（等价于短期记忆增强版） |
| `UserId + AgentId` | 用户 + Agent 交叉 — 用户对该 Agent 的所有历史         |

Inkwell 推荐使用 `AgentId` 级别：同一 Agent 的所有会话历史都可以被检索到。

#### 9.3.5 Agent 集成

通过 `AIContextProviders` 挂载到 Agent：

```csharp
var memoryProvider = new ChatHistoryMemoryProvider(
    vectorStore,
    collectionName: $"memory_{agentId}",
    vectorDimensions: 1536,
    options: new ChatHistoryMemoryProviderOptions
    {
        MaxSearchResults = 5,
        MinRelevanceScore = 0.7,
        RetrievalMode = ChatHistoryMemoryRetrievalMode.BeforeAIInvoke
    });

AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    ChatHistoryProvider = CreateDefaultChatHistoryProvider(20),
    AIContextProviders = [memoryProvider]
});
```

#### 9.3.6 记忆生命周期

| 阶段         | 行为                                                                           |
| ------------ | ------------------------------------------------------------------------------ |
| Agent 运行前 | `ChatHistoryMemoryProvider.InvokingAsync` 搜索相关记忆并注入消息               |
| Agent 运行后 | `ChatHistoryMemoryProvider.InvokedAsync` 将当前用户消息 + Agent 回复写入向量库 |
| 跨 session   | 下次对话时自动搜索到前述存储的记忆                                             |
| 清理         | 按时间或 collection 删除过期记忆                                               |

### 9.4 短期 + 长期记忆协作

```
用户发送 "帮我写一篇关于量子计算的文章"
    ↓
┌─ ChatHistoryProvider.InvokingAsync
│  返回最近 20 轮对话（短期记忆）
│
├─ ChatHistoryMemoryProvider.InvokingAsync
│  向量搜索 → 找到 3 个月前写过的 "量子纠错算法综述"（长期记忆）
│  注入为系统消息: "参考历史：用户曾撰写过量子纠错相关文章..."
│
├─ LLM 生成回复（同时参考短期上下文 + 长期记忆）
│
├─ ChatHistoryProvider.InvokedAsync
│  将本轮对话存入 session（短期）
│
└─ ChatHistoryMemoryProvider.InvokedAsync
   将本轮对话嵌入向量存储（长期）
```

---

## 10. 实施计划

### 阶段 1：后端持久化

1. 新增 `ChatSessionEntity` + `ChatMessageEntity` 到 `InkwellDbContext`
2. 扩展 `ISessionPersistenceProvider` 接口
3. 实现 `EfCoreSessionPersistenceProvider`
4. 适配 `InMemorySessionPersistenceProvider`（新增方法）
5. 实现 `MapAGUIWithSession`
6. 替换 `Program.cs` 中所有 `MapAGUI` -> `MapAGUIWithSession`
7. 所有 Agent 统一配 `InMemoryChatHistoryProvider`

### 阶段 2：API + 前端

1. 新增 `SessionsController`（6 个端点）
2. 前端 `useAGUIAgent` 改造（只发当前消息）
3. 新增 `useSessionList` Hook
4. 对话页面集成会话列表侧栏

### 阶段 3：聊天裁剪

1. 对话型 Agent 升级为 `PipelineCompactionStrategy`
2. 配置 `SummarizationCompactionStrategy` 接入 `IChatClient`
3. 验证裁剪后的对话质量（摘要是否保留关键信息）

### 阶段 4：长期记忆

1. 注册 `IEmbeddingGenerator` 和 `VectorStore`
2. 创建 `ChatHistoryMemoryProvider` 实例
3. Writer / Coordinator 挂载 Memory Provider
4. 验证跨 session 语义检索效果

### 阶段 5：体验优化

1. 会话标题自动生成（LLM 总结）
2. 会话搜索
3. 会话导出（Markdown / JSON）
4. 过期会话 / 过期记忆自动清理
