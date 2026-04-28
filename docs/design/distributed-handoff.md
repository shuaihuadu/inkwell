# 分布式 Handoff：基于 A2A 协议的跨容器/跨服务 Agent 协作

> 基础组件：Microsoft Agent Framework（MAF）的 `Microsoft.Agents.AI.A2A` / `Microsoft.Agents.AI.Hosting.A2A` 两个程序集 + `HandoffWorkflowBuilder`。
>
> 关联现状：Inkwell 当前在 `SmartRoutingBuilder` 中以**进程内**方式实现 Coordinator → Writer/SEO/Translator 的 Handoff；`Inkwell.A2AServer` 项目已存在但尚未承担专家 Agent 角色。

## 1. 问题与目标

现实业务里，参与 Handoff 的 Agent 经常分布在不同进程、不同容器、甚至不同团队维护的不同服务中，比如：

- Writer 由内容团队维护，模型对接 Azure OpenAI
- SEO 由营销团队维护，模型对接国内厂商
- Translator 由国际化小组维护，部署在另一个集群

需求是：**让 Triage（Coordinator）服务在不感知部署位置的前提下完成路由与 Handoff**，同时尽量沿用 MAF 已有的编排原语，不重复发明。

目标：

1. 跨进程 Handoff 与本地 Handoff 在编程模型上**完全等价**。
2. 复用 MAF 的工具调用 + Workflow 状态机机制，不引入第三方编排引擎。
3. 服务发现、可观测、鉴权、失败恢复等工程问题有清晰的归属。

## 2. 关键洞察

`HandoffWorkflowBuilder.WithHandoff(source, target)` 接受的两个参数都是 `AIAgent` 抽象。MAF 提供的 `A2AAgent : AIAgent` 把"远端 A2A 端点"包成本地可调用的 `AIAgent`，因此**分布式 Handoff 在 MAF 里不是新概念，而是把参与者的实现从 `ChatClientAgent` 替换为 `A2AAgent` 即可**。

> A2A 是 Agent-to-Agent 开放协议，基于 HTTP + SSE + JSON-RPC，自带 `AgentCard`（描述 name/description/capabilities/skills），是天然的服务契约与发现入口。

## 3. Agent 异构混编：Workflow 抽象的统一与边界

上一节得出的“参与者只是 `AIAgent`”这条规律可以推广到所有 Agent 实现，而不仅是 A2A。落地时要分清三层“统一”。

### 3.1 哪些 Agent 都能进 Workflow

MAF 的 Workflow 节点最终归到两类：自定义 `Executor<TIn[, TOut]>` 与 `AIAgentHostExecutor`（由 `agent.AsExecutor()` 包出来）。所有面向 Agent 的编排原语（`HandoffWorkflowBuilder` / `AgentWorkflowBuilder.CreateGroupChatBuilderWith` / `WithHandoff` / `AddParticipants` / `BindAsExecutor` …）签名上接的都是 `AIAgent`，不绑死任何具体厂商。`AIAgent` 抽象只规定三件事：`RunAsync` / `RunStreamingAsync` / `CreateSessionAsync`。

| Agent 类型 | 实现来源 | 部署形态 |
|---|---|---|
| 本地 LLM Agent | `ChatClientAgent`（基于 `IChatClient`） | 进程内 |
| Azure OpenAI / OpenAI / Foundry / Anthropic / Bedrock / Ollama / DeepSeek / Qwen ... | 都是 `IChatClient` 的不同实现，套同一个 `ChatClientAgent` | 进程内 |
| Foundry 托管 Agent | `Microsoft.Agents.AI.Foundry` 提供的 Agent 类型 | 远端托管 |
| Claude Agent / Anthropic Managed | 通过对应 SDK 适配的 `IChatClient` 或专用 Agent 类 | 进程内或远端 |
| OpenAI Assistants / Responses | OpenAI Assistants 实现包成 `AIAgent` | 远端托管 |
| 任意 A2A 端点 | `A2AAgent`（包客户端） | 跨进程/跨服务 |
| 自定义 Agent | 继承 `AIAgent` | 任意 |

也就是说，**Workflow 不区分 Agent 是在进程内跑、OpenAI 后端跑、Foundry 托管，还是远端 A2A 端点**。区别在于“`AIAgent` 子类内部是怎么 `RunAsync` 的”，对编排者透明。

### 3.2 三层“统一”

这是关键，否则容易以为只要包成 `AIAgent` 就万事大吉。

**第 1 层：调用接口统一 —— 总成立**

`agent.RunAsync()` / `agent.AsExecutor()` / 塞进 Builder。这一层是 100% 抽象住的。

**第 2 层：消息载体统一 —— 大部分成立，少数有差异**

`ChatMessage` / `IList<ChatMessage>` 是大家共同的语义。但是：

- **工具调用**：本地 `ChatClientAgent` 可以挂 `AITool`（你的 C# 函数）；远端 Foundry/Claude/OpenAI Managed Agent 用的是它们各自托管的工具，**不一定能从客户端直接挂工具进去**——这些工具由对方平台在创建 Agent 时确定。
- **结构化输出**：`ChatResponseFormat.ForJsonSchema<T>()` 在多数 IChatClient 上有效，远端 Managed Agent 的支持需按平台逐个确认。
- **多模态附件**：`UriContent` / `DataContent` 各家支持程度不一致。

**第 3 层：状态/能力对等 —— 不一定成立，必须显式对齐**

这一层最容易踩坑：

| 能力 | 本地 ChatClientAgent | Foundry / OpenAI Managed | A2A Remote |
|---|---|---|---|
| 会话状态归属 | 由你持有（middleware 持久化） | 由对方平台持有（thread / assistantThread） | 由远端服务持有（contextId） |
| HITL（RequestPort） | Workflow 层正常工作 | 远端节点内部触发的 HITL **不会**自动透传到 Triage | 同 Foundry，需自定义协议传递 |
| Checkpoint | 完整保存图状态 | 仅保存图状态，远端 thread 中间态由对方平台持有 | 同 Foundry |
| 取消 / 超时 | CancellationToken 直传 | 取决于 SDK 实现 | HTTP 超时 + 客户端取消 |
| 可观测 | 直接 ActivitySource | OTLP 链路要看 SDK 是否传 W3C TraceContext | `A2AAgent` 已内建 span |
| 失败语义 | 异常即上抛 | 平台错误码 + 重试策略 | HTTP 错误 + 自定义重试策略 |

### 3.3 Workflow 里的混合编排

实际场景里很常见的混合：

```
Workflow:
  Coordinator      = ChatClientAgent  (本地 IChatClient，便宜的小模型路由)
  WriterExpert     = ChatClientAgent  (本地，对接 Azure OpenAI)
  SeoExpert        = A2AAgent         (跨进程，团队 B 维护)
  ResearchExpert   = FoundryAgent     (Foundry 托管，含 Code Interpreter / File Search)
  TranslatorExpert = OpenAIAssistant  (OpenAI Assistants 托管)
```

`WithHandoff(coordinator, writer)` / `WithHandoff(coordinator, seo)`... 写起来一模一样。**编排层完全统一，差异只在适配层和工程治理上**。

### 3.4 几个反直觉但重要的点

1. **托管 Agent ≠ 省事**：能力强（自带工具 / 知识 / RAG）但失去对内部状态的控制权，恢复语义、可观测、成本归集都更复杂。
2. **Workflow 不是 Pub/Sub 总线**：Workflow 是有向图状态机，所有节点共享同一个 run lifetime。需要“事件驱动 + 长生命周期 + 多租户独立”的协作时，应使用消息队列 + Durable Workflow，而不是把 N 个远端 Agent 全堆进一个图里。
3. **位置不是关键，语义契约才是**：远端 Agent 输入/输出格式、可用工具、状态副作用清单必须像 API 一样有版本、有契约。MAF 给了 `AgentCard` 这层壳，业务字段（SEO Agent 期望什么样的 prompt、返回什么样的 JSON）需要业务侧自己治理。

### 3.5 在 Inkwell 中的演进顺序

按“由近及远”的成本顺序：

1. **进程内多模型**：现状即如此，`SmartRoutingBuilder` 中三个 `ChatClientAgent` 走不同 IChatClient（这是最省的“分布”）。
2. **A2A 自家服务**：把专家拆到 `Inkwell.A2AServer`（见后文落地路径）。MAF 直接支持。
3. **Foundry 托管 Agent**：Coordinator 留本地，把依赖 Foundry 内置工具（File Search / Code Interpreter）的专家放 Foundry。等于业务把“重工具 / 重知识”外包给 Foundry，把“路由 + 流程 + HITL”留在自己手里。
4. **Claude / OpenAI Managed**：把它们当作“远端能力供应商”，注意工具与状态由它们自己持有。

一句话：**Workflow 是“哪些 Agent 怎么协作”，不在乎“Agent 跑在哪儿”**；但跑在哪儿决定了“会话 / HITL / 失败 / 工具”这些工程语义的边界，这是架构决策时必须显式取舍的部分。

## 4. 整体拓扑

```
                 +---------------------------------------------+
                 |   Triage 服务（HandoffWorkflowBuilder）      |
                 |                                             |
   用户 -----> |   Coordinator Agent（本地 ChatClientAgent）  |
                 |      |                                       |
                 |      | 工具：transfer_to_writer/seo/...       |
                 |      v                                       |
                 |   HandoffAgentExecutor                       |
                 +--+----------+-----------+--------------------+
                    | A2A      | A2A       | A2A
                    v          v           v
              +---------+ +---------+ +-----------+
              | Writer  | |   SEO   | | Translator|
              | Service | | Service | |  Service  |
              | (容器)  | | (容器)  | |  (容器)   |
              +---------+ +---------+ +-----------+
```

要点：

- 编排状态机和决策权全部留在 Triage 服务。
- 远端服务只负责"接到一段消息 → 跑自己的 Agent → 返回结果"，不感知 Handoff 的存在。
- Coordinator 仍是本地 `ChatClientAgent`，因为它要消费 HITL、流式回写前端，与 Web 层耦合更紧。

## 5. 服务端：把任意 Agent 暴露为 A2A 端点

每个专家 Agent 部署成独立服务（或同一镜像通过环境变量区分加载哪个 Agent）。

```csharp
// Inkwell.WriterService / Program.cs（伪代码骨架）
builder.Services.AddAIAgent("writer", sp =>
    sp.GetRequiredKeyedService<IChatClient>(AIProviderKeys.Primary)
      .CreateAIAgent(name: "Writer", instructions: "...你是文案作家..."));

WebApplication app = builder.Build();
app.MapA2A("/agents/writer", agentName: "writer");
app.Run();
```

`MapA2A()` 同时挂出两个端点：

| 路径 | 用途 |
|---|---|
| `/.well-known/agent.json` | AgentCard：name/description/skills/version |
| `POST /agents/writer` | A2A JSON-RPC + SSE 流，承担实际对话 |

## 6. 客户端：把 A2A 端点包成 AIAgent，加入 Handoff

```csharp
A2AAgent writer = await new A2AClient(new Uri(writerUrl))
    .GetAIAgentAsync(name: "Writer");
A2AAgent seo = await new A2AClient(new Uri(seoUrl)).GetAIAgentAsync(name: "Seo");
A2AAgent translator = await new A2AClient(new Uri(transUrl)).GetAIAgentAsync(name: "Translator");

ChatClientAgent coordinator = chatClient.CreateAIAgent(
    name: "Coordinator",
    instructions: "根据用户意图选择写作/SEO/翻译专家...");

Workflow workflow = new HandoffWorkflowBuilder(coordinator)
    .WithHandoff(coordinator, writer)
    .WithHandoff(coordinator, seo)
    .WithHandoff(coordinator, translator)
    .Build();
```

跟 Inkwell 现有的 `SmartRoutingBuilder` 比，**只有专家 Agent 的构造方式变了**，整个 Workflow 拓扑构建代码不变。

## 7. 路由策略选择

| 策略 | MAF 原语 | 适用场景 |
|---|---|---|
| LLM 路由（默认） | `HandoffWorkflowBuilder` —— Coordinator 通过 `transfer_to_X` 工具调用决定下一个 Agent | 意图模糊、需自然语言理解 |
| 规则 / 分类器路由 | `WorkflowBuilder.AddSwitch` + `AddCase<T>(predicate, target)` | 入口意图明确（关键词、类型字段） |
| 混合 | 前置 Classifier executor 把请求分桶 → 桶里再让 Coordinator 精细路由 | 流量大、需省 token |

## 8. 工程问题清单

### 7.1 会话延续（context propagation）

A2A 协议有 `contextId`，`A2AAgentSession` 会复用同一 contextId 让远端 Agent 看到完整对话上下文。但要注意：**远端 Agent 的对话状态归它自己持有**。Inkwell 现有 `WorkflowChatClient` 把 checkpoint 写成 JSON 并塞进 `_RUN_:` 标记的方案对远端 Agent 不直接生效。

策略二选一，不要混：

- **集中式**：Coordinator 一侧统一持久化（远端只接受瞬态调用），简单但远端无记忆。
- **分布式**：远端 Agent 各自做持久化，Coordinator 只透传 contextId，复杂但符合微服务边界。

Inkwell 推荐先走集中式：现有 `SessionPersistenceMiddleware` 不动，远端 Agent 当作纯函数。

### 7.2 失败语义

| 失败类型 | 推荐处理 |
|---|---|
| 网络错误 / 超时 | `HandoffAgentExecutor` 上抛工作流错误，由 `WorkflowChatClient` 转成可见错误消息 |
| 远端 4xx | Coordinator 看到错误后由 LLM 决定重路由（fallback to 另一专家）或终止 |
| 长任务 | 当前 `A2AAgent` 仅支持同步 message，A2A 异步 task 模式还没接入；遇到长耗时业务需评估 |

### 7.3 服务发现

| 方案 | 适用 |
|---|---|
| 静态注册表 | 配置文件 / DI 中维护 `agentName → Url`，启动时拉一次 AgentCard 校验 |
| AgentCard 动态发现 | Triage 周期性拉 `/.well-known/agent.json`，按 `skills` 决定 Coordinator 工具集 |

多团队多服务场景建议动态发现 + 缓存 + 后台心跳。

### 7.4 可观测

A2A 客户端走 `HttpClient`，OpenTelemetry 的 W3C TraceContext 默认会传播；`A2AAgent.RunAsync` 自带 `ActivitySource` span。Inkwell 接 Aspire Dashboard 后，把远端服务的 OTLP exporter 指向同一个 dashboard，即可看到完整跨进程链路。

### 7.5 鉴权

A2A 协议本身不规定鉴权。落地方案：

- 内网：mTLS + 服务网格，最干净
- 跨网：`A2AClientOptions.HttpClient` 注入鉴权 header（短期 Bearer / API Key），远端用中间件验证

无论哪种，远端 Agent 端口都不要直接暴露公网。

### 7.6 工具与代码可移植性

进程内 Handoff 时 Agent 使用的本地 Skill / Tool 在跨进程后**不会自动可用**。两种思路：

- 工具留在远端 Agent 自己进程内（推荐）：Triage 完全不感知 Tool 实现。
- 工具下沉到 MCP server：所有 Agent 通过 `IChatClient` 的 MCP 桥接调用。

## 9. 在 Inkwell 中的落地路径

分阶段，避免一步到位带来的过度工程。

### 阶段一：复用 Inkwell.A2AServer 暴露三位专家

在 `Inkwell.A2AServer` 里把 `WriterAgent / SeoAgent / TranslatorAgent` 注册为独立的 A2A 端点：

```
/agents/writer        Writer 专家
/agents/seo           SEO 专家
/agents/translator    翻译专家
```

实现要点：

- 复用现有 `IChatClient` Provider 与 Agent 工厂。
- 镜像保持单一，靠环境变量 `INKWELL_A2A_AGENT=writer|seo|translator|all` 决定加载范围。
- `docker-compose` 增加 `inkwell-writer / inkwell-seo / inkwell-translator` 三个 service（同镜像不同环境变量），共用一个 SQL Server、一个 Aspire Dashboard。

### 阶段二：新增 DistributedSmartRoutingBuilder

不动现有 `SmartRoutingBuilder`，新增并列实现：

- 输入：三个 A2A URL（从配置读取）
- 输出：与 `SmartRoutingBuilder` 完全等价的 `Workflow`
- 注册为新工作流 `smart-routing-distributed`，使用说明文档对比两种实现的差异

### 阶段三：演示 / 教学价值

同时拥有 `smart-routing`（进程内）与 `smart-routing-distributed`（跨进程）两个工作流后，Inkwell 即可作为**完整的对比案例**：

- 编排代码几乎相同 → 印证 MAF 抽象的稳定性
- 部署形态截然不同 → 体现 A2A 在微服务化场景的价值
- 失败模式、可观测、鉴权差异 → 提供学习材料

## 10. 风险与未决问题

1. **A2A 异步 task 模式尚未接入 `A2AAgent`**：长耗时专家（比如批量翻译）短期需要走同步 + 客户端超时；想要可恢复就得自建状态机。
2. **HITL 跨进程语义**：远端 Agent 内部触发的 `RequestPort` 当前不会原样回到 Triage 的 SSE 流，需要在 A2A 协议层定义"远端 HITL 请求 → 客户端 SSE 转发"的传递方式。Inkwell 层面建议：**HITL 节点只放在 Coordinator 一侧**，远端 Agent 不要含 HITL。
3. **Checkpoint 跨进程**：`Workflow.SaveCheckpointAsync` 只保存 Triage 这一侧的图状态；远端 Agent 在自己进程内的中间态不在 checkpoint 里。这要求"远端 Agent 调用必须是幂等且无中间态"，否则恢复点会失效。

## 11. 与 MAF 生态的关系

| MAF 提供 | Inkwell 应承担 |
|---|---|
| `A2AAgent` / `A2AClient` —— 客户端抽象 | DI 封装与服务发现策略 |
| `MapA2A` —— 服务端宿主 | 镜像构建与多 Agent 复用 |
| `HandoffWorkflowBuilder` —— 编排原语 | DistributedSmartRoutingBuilder 包装 |
| OpenTelemetry 内建 | Aspire Dashboard 接线、跨服务 trace 验证 |
| 协议规范 + AgentCard | 内部 AgentCard 字段约定（version / skills 名称空间） |

## 12. 后续动作建议

- [ ] 在 `Inkwell.A2AServer` 里跑通单一 Writer Agent，验证 AgentCard 与 `POST /agents/writer` 的最小回路。
- [ ] 设计 `DistributedSmartRoutingBuilder`，给配置 schema 留位（数组：`{ name, url, role }`）。
- [ ] 写一个最小的端到端集成测试：本地 docker-compose 起 4 个容器（Triage + 3 个专家），用 SmartRoutingBuilder 与 DistributedSmartRoutingBuilder 跑相同输入，对比输出。
- [ ] 评估 HITL 与 Checkpoint 跨进程方案，必要时为分布式版本单独写一套 capability 标记。
