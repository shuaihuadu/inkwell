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

## 3. 整体拓扑

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

## 4. 服务端：把任意 Agent 暴露为 A2A 端点

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

## 5. 客户端：把 A2A 端点包成 AIAgent，加入 Handoff

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

## 6. 路由策略选择

| 策略 | MAF 原语 | 适用场景 |
|---|---|---|
| LLM 路由（默认） | `HandoffWorkflowBuilder` —— Coordinator 通过 `transfer_to_X` 工具调用决定下一个 Agent | 意图模糊、需自然语言理解 |
| 规则 / 分类器路由 | `WorkflowBuilder.AddSwitch` + `AddCase<T>(predicate, target)` | 入口意图明确（关键词、类型字段） |
| 混合 | 前置 Classifier executor 把请求分桶 → 桶里再让 Coordinator 精细路由 | 流量大、需省 token |

## 7. 工程问题清单

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

## 8. 在 Inkwell 中的落地路径

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

## 9. 风险与未决问题

1. **A2A 异步 task 模式尚未接入 `A2AAgent`**：长耗时专家（比如批量翻译）短期需要走同步 + 客户端超时；想要可恢复就得自建状态机。
2. **HITL 跨进程语义**：远端 Agent 内部触发的 `RequestPort` 当前不会原样回到 Triage 的 SSE 流，需要在 A2A 协议层定义"远端 HITL 请求 → 客户端 SSE 转发"的传递方式。Inkwell 层面建议：**HITL 节点只放在 Coordinator 一侧**，远端 Agent 不要含 HITL。
3. **Checkpoint 跨进程**：`Workflow.SaveCheckpointAsync` 只保存 Triage 这一侧的图状态；远端 Agent 在自己进程内的中间态不在 checkpoint 里。这要求"远端 Agent 调用必须是幂等且无中间态"，否则恢复点会失效。

## 10. 与 MAF 生态的关系

| MAF 提供 | Inkwell 应承担 |
|---|---|
| `A2AAgent` / `A2AClient` —— 客户端抽象 | DI 封装与服务发现策略 |
| `MapA2A` —— 服务端宿主 | 镜像构建与多 Agent 复用 |
| `HandoffWorkflowBuilder` —— 编排原语 | DistributedSmartRoutingBuilder 包装 |
| OpenTelemetry 内建 | Aspire Dashboard 接线、跨服务 trace 验证 |
| 协议规范 + AgentCard | 内部 AgentCard 字段约定（version / skills 名称空间） |

## 11. 后续动作建议

- [ ] 在 `Inkwell.A2AServer` 里跑通单一 Writer Agent，验证 AgentCard 与 `POST /agents/writer` 的最小回路。
- [ ] 设计 `DistributedSmartRoutingBuilder`，给配置 schema 留位（数组：`{ name, url, role }`）。
- [ ] 写一个最小的端到端集成测试：本地 docker-compose 起 4 个容器（Triage + 3 个专家），用 SmartRoutingBuilder 与 DistributedSmartRoutingBuilder 跑相同输入，对比输出。
- [ ] 评估 HITL 与 Checkpoint 跨进程方案，必要时为分布式版本单独写一套 capability 标记。
