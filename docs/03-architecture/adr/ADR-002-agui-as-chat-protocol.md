---
id: ADR-002
title: 对话面采用 AG-UI 协议（与 REST 数据面分离）
stage: H2
status: accepted
authors:
  - name: H2-ArchitectAdvisor
    role: agent
date: 2026-05-07
upstream:
  - architecture-custom-agent
  - tech-selection-custom-agent
supersedes: []
superseded-by: []
---

# ADR-002：对话面采用 AG-UI 协议（与 REST 数据面分离）

## 上下文

[REQ-004 试运行](../../01-requirements/requirements.md) / [P2 试运行下半屏](../../01-requirements/ui-spec.md)需要前后端之间流式传输 Agent 的 **lifecycle / 文本增量 / 工具调用 / 工具结果 / 错误**等事件。如自研 SSE 事件 schema，每加一种事件类型都要前后端同步改 contract，长尾成本高。

用户偏好 7：
> 前后端使用 REST API 交互，但是 Agent 对话需要使用 AGUI

经 H2 反问 Q1 = A 确认 AGUI = AG-UI Protocol（[docs.ag-ui.com](https://docs.ag-ui.com/)）。

## 决策

- **数据面**：REST(JSON, axios)，端点 `/api/agents` / `/api/skills` / `/api/tools` / `/api/uploads` 等所有 CRUD + 文件上传
- **对话面**：AG-UI Protocol（默认 SSE transport），端点 `POST /api/agui/run`，请求体 `RunAgentInput`，响应 SSE 流输出 [16 种 EventType](https://docs.ag-ui.com/concepts/architecture) 子集（本期实现 lifecycle / text-message / tool-call / messages-snapshot / RUN_ERROR；STATE_* 留 vNext）
- **客户端**：前端用 `@ag-ui/client`（HttpAgent + SSE）+ React 自建对话视图（不绑 CopilotKit UI，见 [OQ-A-007](../open-questions-arch.md)）
- **服务端**：走 MAF 1st-party AG-UI integration 路径（[ADR-001](./ADR-001-microsoft-agent-framework.md)），具体包：
  - [`Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`](https://github.com/microsoft/agent-framework/tree/main/dotnet/src/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore)（`AGUIEndpointRouteBuilderExtensions` / `ServiceCollectionExtensions` / `AGUIServerSentEventsResult` / `AGUIChatResponseUpdateStreamExtensions` / `AGUIJsonSerializerOptions`）
  - [`Microsoft.Agents.AI.AGUI`](https://github.com/microsoft/agent-framework/tree/main/dotnet/src/Microsoft.Agents.AI.AGUI)（`AGUIChatClient` / `AGUIHttpService`，作为共享协议类型，调用方场景使用）
  - 状态：[Preview](https://learn.microsoft.com/en-us/agent-framework/integrations/)（非 Released），包变化由 [RISK-001](../risk-analysis.md) 跟踪；如包未来废弃或独立 AG-UI .NET SDK GA，开 ADR 替换
  - Fallback（保留可选项，不优先）：自实现 SSE handler 输出 16 种 EventType 子集（≤ 5 人天，hosting 包源码作为实现参考）

## 备选项

| 备选 | 放弃理由 |
| --- | --- |
| 自研 SSE 事件 schema（含 OpenAI Responses API 风格 delta） | 自维护 schema 长尾成本高；放弃 AG-UI 生态（CopilotKit / dojo / 各客户端 SDK） |
| SignalR | .NET 友好但浏览器需 `@microsoft/signalr` 客户端，与 AntD + React 生态不顺；与 MAF 集成不是 1st-party |
| gRPC streaming | 浏览器需 grpc-web 转换；AG-UI 已覆盖典型对话事件 |
| WebSocket（自研协议） | 同自研 SSE 问题 + 双向通信本期不需要 |
| AG-UI 用 WebSocket transport（不用 SSE） | AG-UI 协议层一致；但 K8s ingress SSE 调优文档更全；浏览器 EventSource 原生支持 SSE，无需 polyfill |

## 后果

### 正面

- 16 种 EventType 标准覆盖 lifecycle / text / tool / state / messages / raw / custom，长尾事件零自维护
- MAF 1st-party 集成（包 `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` 已固化，[OQ-A-003](../open-questions-arch.md) 已关闭）规避独立 [AG-UI .NET SDK 仅 In Progress](https://docs.ag-ui.com/) 风险
- 服务端落地走 ASP.NET Core 标准 "DI 注册 + Endpoint 映射" 模式（`services.AddAgent<...>()` + `app.MapAGUIAgent(...)`），学习曲线低
- vNext 加 STATE_SNAPSHOT / STATE_DELTA 支持 Agent 状态共享类长尾场景，无需改契约
- transport-agnostic：本期 SSE（hosting 包默认输出 `AGUIServerSentEventsResult`），vNext 切 WebSocket / HTTP 二进制只换 transport 不改 schema

### 负面

- AG-UI 协议较新（2024-2025 起步），生态仍在演进；学习成本中等
- K8s Ingress 默认 buffer SSE 流，需手动调优（[RISK-006](../risk-analysis.md)）
- MAF AG-UI hosting 包仍是 [Preview](https://learn.microsoft.com/en-us/agent-framework/integrations/)，Preview → GA 期间可能数次 breaking change（[RISK-001](../risk-analysis.md)）

### 中性

- 数据面 REST + 对话面 AG-UI 双协议，前端需要分别封装；axios 与 AG-UI 客户端不混用，反而降低耦合

## 状态

`accepted` · 2026-05-07
