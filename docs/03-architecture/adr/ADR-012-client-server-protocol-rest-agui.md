---
id: ADR-012-client-server-protocol-rest-agui
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-08
updated: 2026-05-09
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - ADR-002
  - ADR-003
  - ADR-011
downstream: []
---

# ADR-012 客户端↔后端协议：REST + AG-UI Protocol

## 上下文

[Q-A6](../open-questions-arch.md) 用户答：“REST + AG-UI Protocol。”AG-UI Protocol 是 [microsoft/agent-framework](../../../../../microsoft/agent-framework/) 提供的 Agent 与 UI 之间的事件协议（详见 [Microsoft.Agents.AI.Hosting.AGUI.AspNetCore](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore/)），定义了 message / tool_call / state_delta / lifecycle 等事件类型。

[OQ-A002 closed §A](../open-questions-arch.md) 明确：v1 跨锁屏走“Electron 主进程长 SSE”路径，不采用 Run resume cursor 重放机制。本 ADR 同步反转：SSE 单向事件流不走事件库 + cursor，仅提供“当前 Run 最终状态”重拉 endpoint作为兑底。

需要支持的语义：

- 同步 REST（CRUD：Agent / Skill / 知识库 / 编排 / Token / 审计日志查询）
- 流式 Run（聊天 / 编排执行）→ 模型 token 流式输出 + 工具调用事件 + trace 事件
- 跨锁屏体验（[ADR-011](./ADR-011-auto-lock-with-inflight-task-survival.md)）仅依赖主进程长 SSE + 本地环缓 + 兑底重拉 endpoint

## 决策

**客户端↔后端协议 = REST（CRUD）+ AG-UI Protocol over SSE（Run 事件流）。不引入 Run resume cursor / RunEventStore；主进程长 SSE 丢连后仅从后端“当前 Run 状态 endpoint”重拉最终状态重渲染。**

- REST：JSON over HTTP，OpenAPI 3 文档生成（[Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) 或 [Microsoft.AspNetCore.OpenApi](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi)）。
- AG-UI：
  - 端点：`POST /api/runs`（创建 Run，返回 run_id + 立即开始的 SSE）。`GET /api/runs/{id}/state`（主进程重连兑底拉取当前状态快照）。不提供 `POST /api/runs/{id}/resume?cursor=`（原 §C 方案接口，本 ADR 反转后不引入）。
  - 事件 schema 沿用 AG-UI 标准（[microsoft/agent-framework AGUI 包](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.AGUI/)）。
  - 持久化：**不** 持久化 AG-UI 事件流。后端仅保持 Run 本身状态于 [Microsoft.Agents.AI.DurableTask](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/)；Run 产出的 message / tool_call 结果以业务形式写 [`Inkwell.Conversations`](../../01-requirements/repo-impact-map.md)（对话沉淀）与 [`Inkwell.Traces`](../../01-requirements/repo-impact-map.md)（trace span 仅走 OTel）。
- 错误模型：REST 用 [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807)；AG-UI 用 `event: error` + JSON body（含 error_code / message）。
- 鉴权：内部 API 走客户端会话 cookie（v1 单用户场景）；公开 API 走 [ADR-007 单 Token](./ADR-007-public-api-token-auth.md)。
- 序列化：[System.Text.Json](https://learn.microsoft.com/dotnet/api/system.text.json)，camelCase；客户端 [Zod](https://zod.dev/) 校验 schema。
- SSE 心跳：`X-Accel-Buffering: no` + 15 s 一个心跳事件；客户端主进程 30 s 超时后主动重连 → 重连后调用 `GET /api/runs/{id}/state` 拉取当前状态，流重新从连接点后续。

## 备选项

### 备选 A：REST + GraphQL

- **放弃理由**：(1) GraphQL subscription 需要额外 transport（WebSocket / SSE）— 与 AG-UI 重复；(2) 团队 GraphQL 经验弱于 REST；(3) v1 数据查询场景不存在 N+1 / 字段级 customization 需求。

### 备选 B：纯 gRPC（gRPC-Web 客户端）

- **放弃理由**：(1) gRPC 流式语义与 AG-UI 事件协议需要做适配层；(2) 浏览器 / Electron Renderer 上的 gRPC-Web 体验不如 SSE 流畅；(3) 与 [microsoft/agent-framework](../../../../../microsoft/agent-framework/) 的 hosting 包默认 SSE 模式不一致。

### 备选 C：REST + WebSocket（自建协议）

- **放弃理由**：(1) 自建协议需要重新发明 AG-UI 已经标准化的事件类型；(2) 失去与 microsoft/agent-framework 生态对接的能力（如 [@ag-ui/client](https://github.com/ag-ui-protocol) JavaScript SDK）；(3) cursor 续传机制需要自实现。

### 备选 D：纯 AG-UI（不做 REST）

- **放弃理由**：CRUD 操作走事件流是过度设计；REST 是浏览器 / 工具链 / 调试器的最低门槛。

## 后果

### 正面

- 与 [ADR-003 Microsoft Agent Framework](./ADR-003-agent-engine-microsoft-agent-framework.md) 强配套：后端 hosting 一行 `builder.Services.AddAGUIHosting()` 即可启用。
- AG-UI 事件类型经过 Microsoft 标准化（message / tool_call / state_delta / lifecycle），客户端事件渲染语义清晰。
- 不引入 RunEventStore / cursor / replay，v1 实现路径最短；跨锁屏体验交给 [ADR-011](./ADR-011-auto-lock-with-inflight-task-survival.md) 主进程长 SSE 路径。
- 公开 API（[ADR-007](./ADR-007-public-api-token-auth.md)）也是 REST，第三方调用门槛低。
- OpenAPI 文档自动生成 → SDK 生成 / 调试器都有现成工具链。

### 负面

- 双协议（REST + AG-UI）需要客户端两套代码路径；通过把 AG-UI 客户端封装到 [`Inkwell.AGUI.Client`](../../01-requirements/repo-impact-map.md) 模块缓解。
- AG-UI 当前仍在演进，breaking change 风险存在；通过锁定具体版本 + 升级前跑全量集成测试缓解。
- 不持久化事件流 → 客户端主进程丢连 × 环缓区溢出 → 中间过程事件丢，仅能从 `GET /api/runs/{id}/state` 拉最终状态重渲染。详见 [RISK-007](../risk-analysis.md)。
- SSE 在企业代理 / 防火墙穿透方面偶尔有问题（缓冲 / 超时）；通过设置 `X-Accel-Buffering: no` + 心跳事件 + 客户端自动重连缓解。

### 中性

- 不再定义 cursor 语义；AG-UI 事件仅在流上流动，不进库。
- AG-UI 不携带业务级权限信息 — 鉴权由 REST middleware（cookie / Bearer Token）在握手阶段完成，AG-UI 流上不再二次校验。

## 状态

- **状态**：accepted（接受 [OQ-A002 closed §A](../open-questions-arch.md) 决议：Run resume cursor 不引入 v1）
- **首次发布**：2026-05-08（初版，含 cursor） / 2026-05-09 重写为 §A（汇入兑底状态 endpoint）
- **关联**：supersedes 无；上游 [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) / [ADR-003](./ADR-003-agent-engine-microsoft-agent-framework.md) / [ADR-011](./ADR-011-auto-lock-with-inflight-task-survival.md)
- **置信度**：high（与 microsoft/agent-framework 一等公民能力对齐）
