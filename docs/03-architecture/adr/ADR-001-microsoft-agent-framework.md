---
id: ADR-001
title: 选用 Microsoft Agent Framework 作为 Agent 编排框架
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

# ADR-001：选用 Microsoft Agent Framework 作为 Agent 编排框架

## 上下文

[REQ-004](../../01-requirements/requirements.md)（试运行）/ [REQ-005](../../01-requirements/requirements.md)（gpt-4.1 调用）/ [REQ-011](../../01-requirements/requirements.md)（首批 Tool）/ [REQ-012](../../01-requirements/requirements.md)（vNext MCP）需要一套 Agent 框架完成"组装 system prompt → 注册 Tool → 调用 LLM → 输出事件流"的编排链路。

[AGENTS.md L13~L14](../../../AGENTS.md) 已在项目身份处签字：
> Inkwell 是 Harness Engineering + Microsoft Agent Framework 的 dogfooding 项目：
> 用真实项目验证这套规范+工具链端到端能不能打造一个可工作的"智能体工厂"。

故本决策不是评估"是否选 MAF"，而是决定**基于 MAF 哪个版本** + **基于哪些子包**。

## 决策

- **主包**：`Microsoft.Agents.AI` GA 1.4.0+（NuGet 锁定到 1.4.x minor，关注 [GitHub Releases](https://github.com/microsoft/agent-framework/releases) 节奏）
- **不引入** prerelease 子包：`Microsoft.Agents.AI.Foundry` / `Microsoft.Agents.AI.Workflows.Declarative.Mcp` 等
- **AG-UI 集成**：走 MAF 1st-party AG-UI integration（[docs.ag-ui.com](https://docs.ag-ui.com/) Agent Framework - 1st Party · Supported），具体包：
  - 服务端 hosting：[`Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`](https://github.com/microsoft/agent-framework/tree/main/dotnet/src/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore)
  - 协议层 / 客户端：[`Microsoft.Agents.AI.AGUI`](https://github.com/microsoft/agent-framework/tree/main/dotnet/src/Microsoft.Agents.AI.AGUI)
  - 状态：[Microsoft Learn](https://learn.microsoft.com/en-us/agent-framework/integrations/) 标 "AG UI · Preview"，跟踪见 [RISK-001](../risk-analysis.md)
- **遥测**：启用内置 OpenTelemetry instrumentation，导出 OTLP（[architecture.md 第 10 节](../architecture.md#10-可观测性方案)）

## 备选项

| 备选 | 放弃理由 |
| --- | --- |
| Semantic Kernel | [Microsoft Learn](https://learn.microsoft.com/en-us/agent-framework/overview/) 官方说明 MAF 是 SK 的"直接继任者"；新项目应直接选 MAF，避免后续迁移 |
| AutoGen | 同上，MAF 也是 AutoGen 的继任者 |
| LangChain.NET（社区版） | 社区维护，依赖更新不可控；与 .NET 主流生态偏离 |
| 自研编排 | 与 [AGENTS.md](../../../AGENTS.md) "MAF dogfooding" 项目身份冲突 |
| MAF 拉 prerelease 子包（如 Foundry 1.x-preview） | 本期 MVP 不需要 Foundry Hosted Agents；prerelease breaking change 风险见 [RISK-002](../risk-analysis.md) |

## 后果

### 正面

- 一站式覆盖 Agent / Workflow / Tool / Skill / 中间件 / OTel 所需能力
- 与 Azure OpenAI（[ADR-004 引用 Q6=A](./ADR-004-platform-login-dev-mock.md)）/ AG-UI（[ADR-002](./ADR-002-agui-as-chat-protocol.md)）官方背书集成，规避自研适配
- dogfooding 项目身份沉淀实践经验，反哺 [Harness Engineering 规范](../../../.he/HANDBOOK.md)

### 负面

- MAF 仍处早期演进期；major 升级时可能 breaking（[RISK-002](../risk-analysis.md)）
- 团队需学习 MAF 的 Agent / Workflow / Skill 概念模型，学习曲线中等

### 中性

- vNext MAF 引入新能力（如 Workflow visual designer / DevUI）时可低成本接入

## 状态

`accepted` · 2026-05-07
