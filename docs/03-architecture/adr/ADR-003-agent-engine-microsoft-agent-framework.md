---
id: ADR-003-agent-engine-microsoft-agent-framework
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [Inkwell]
created: 2026-05-08
updated: 2026-05-08
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - ADR-002
downstream:
  - ADR-006
  - ADR-010
  - ADR-012
---

# ADR-003 Agent 执行引擎：Microsoft Agent Framework

## 上下文

Inkwell 的核心是 Agent 平台，需要一个"Agent 执行引擎"来承接：

- [REQ-005 模型选择](../../01-requirements/requirements.md)：v1 必须支持 Azure OpenAI，预留 OpenAI / Claude / Qwen / 智谱接入位
- [REQ-007 Function Calling](../../01-requirements/requirements.md)：后端注册工具，运行期由后端代理调用，可以是MAF的AIFunction
- [REQ-008 Skills](../../01-requirements/requirements.md)：[agentskills.io 格式](https://agentskills.io)，按 Discovery → Activation → Execution 渐进加载
- [REQ-010 多轮对话与长期记忆](../../01-requirements/requirements.md)
- [REQ-012 多 Agent 协作 / 编排](../../01-requirements/requirements.md)：DAG 编排 + Agent 版本锁定
- [REQ-014 调试 / 评测](../../01-requirements/requirements.md)：trace 全链路 + 重放
- [Q-A6 协议](../open-questions-arch.md) AG-UI Protocol 客户端流式

第三步反问 Q-A3 用户答 "Microsoft Agent Framework"。workspace 已加载 [microsoft/agent-framework](../../../../../microsoft/agent-framework/) 作为依赖参考。

## 决策

**Agent 执行引擎锁定为：Microsoft Agent Framework（MAF），.NET 实现，包级别引用如下：**

- `Microsoft.Agents.AI`（核心 Agent 抽象）
- `Microsoft.Agents.AI.Abstractions`（接口契约）
- `Microsoft.Agents.AI.OpenAI`（Azure OpenAI / OpenAI Provider）
- `Microsoft.Agents.AI.Anthropic`（Claude Provider，REQ-005 接入位预留）
- `Microsoft.Agents.AI.Hosting`（ASP.NET Core hosting integration）
- `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`（[ADR-012 AG-UI Protocol](./ADR-012-client-server-protocol-rest-agui.md) 后端 hosting）
- `Microsoft.Agents.AI.Workflows`（[ADR-006 编排画布](./ADR-006-orchestration-canvas-react-flow.md) 后端 DAG 执行引擎）
- `Microsoft.Agents.AI.DurableTask`（编排持久化 + 跨 Pod 重启续作）

**REQ-008 Skills v1 仅静态加载**，不实例化 MAF 的 SkillExecutor / Plugin 执行接口（详见 [ADR-010](./ADR-010-skill-loading-static-only-v1.md)）。

## 备选项

### 备选 A：Semantic Kernel（SK）

- **放弃理由**：(1) Microsoft 已宣布 [Semantic Kernel 与 Azure AI Agent SDK 演进合并为 Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/)，SK 进入维护模式；新功能（如 AG-UI 协议、DurableTask 编排、Workflows DAG）只在 MAF 落地。(2) SK 的 Plugin 概念与 [agentskills.io 格式](https://agentskills.io) 语义不完全对齐，会带来额外适配层。(3) [REQ-014 trace 全链路](../../01-requirements/requirements.md) 在 SK 上需要自建中间件，MAF 内置 OpenTelemetry instrumentation。

### 备选 B：LangChain（.NET 端口 / Python 主线）

- **放弃理由**：(1) LangChain.NET 不是官方实现，社区维护；(2) [REQ-012 DAG 编排](../../01-requirements/requirements.md) 在 LangChain 中需要 LangGraph，进一步引入 Python 跨语言调用；(3) trace / 持久化能力弱于 MAF Workflows + DurableTask。

### 备选 C：自研 Agent 框架

- **放弃理由**：(1) 与 [OQ-006 closed §A](../../01-requirements/open-questions.md) "v1 范围风险已签字" 严重冲突——自研框架的工期不可控；(2) 工具调用 / Token Counting / 错误重试等能力需要从零搭建；(3) 没有任何复用价值。

## 后果

### 正面

- AG-UI Protocol 后端 hosting 一行注册（`builder.Services.AddAGUIHosting()`），无需自建 SSE / cursor 重连机制，与 [ADR-012](./ADR-012-client-server-protocol-rest-agui.md) 强配套。
- DurableTask 提供 [REQ-012 编排](../../01-requirements/requirements.md) 跨 Pod 重启续作能力，AKS 多副本部署不会因 Pod 替换丢失正在执行的 DAG 节点。
- 内置 OpenTelemetry instrumentation（覆盖 LLM 调用 / 工具调用 / Skill 命中 / 编排节点），与 [ADR-013](./ADR-013-observability-otel-self-hosted-grafana.md) 直接对齐。
- [REQ-005 模型预留](../../01-requirements/requirements.md) 通过 MAF 的 Provider 接口（`IChatClient`）一键接入 Azure OpenAI / OpenAI / Anthropic / 等多家。
- [Microsoft.Agents.AI.Mem0](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.Mem0/) 与 [Microsoft.Agents.AI.CosmosNoSql](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.CosmosNoSql/) 提供长期记忆 / 持久化候选实现，REQ-010 路径短。

### 负面

- MAF 的稳定性仍在演进（首发 2024-Q4），breaking change 频率高于 .NET 内置库；通过锁定具体 NuGet 版本 + 升级前跑全量 H4 用例缓解。
- 团队需要学习 MAF 的核心概念（`Agent` / `AgentThread` / `Tool` / `Workflow` / `DurableOrchestrator`）；通过引用 [microsoft/agent-framework dotnet/AGENTS.md](../../../../../microsoft/agent-framework/dotnet/AGENTS.md) + 团队内部 R&D 文档缓解。
- MAF 当前对国内厂商（Qwen / 智谱）尚无官方 Provider；REQ-005 预留接入位 v1 仅做 Azure OpenAI 真接，其他靠自研 `IChatClient` 实现（H3 任务）。

### 中性

- 与 [ADR-002 .NET 10](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) 强绑定——切换运行时即等于切换 Agent 引擎。
- AGENT 版本锁定在 [REQ-012 编排节点](../../01-requirements/requirements.md) 的语义需要 MAF Workflows 的"Agent Snapshot"能力（H3 详细设计验证）。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md)；下游 [ADR-006](./ADR-006-orchestration-canvas-react-flow.md) / [ADR-010](./ADR-010-skill-loading-static-only-v1.md) / [ADR-012](./ADR-012-client-server-protocol-rest-agui.md)
- **置信度**：high（workspace 已加载 microsoft/agent-framework，包结构已验证；与 H1 多条 REQ 直接对齐）
