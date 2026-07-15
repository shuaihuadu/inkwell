---
id: H5-005
title: Agent 会话 · 实施范围
stage: H5
document_type: scope
status: draft
authors:
	- name: GitHub Copilot
		role: agent
reviewers: []
created: 2026-07-15
updated: 2026-07-15
upstream:
	- REQ-010
	- REQ-018
	- NFR-003
	- NFR-005
	- ADR-011
	- ADR-012
downstream:
	- H5-005-A
	- H5-005-B
	- H5-005-C
	- H5-005-D
---

<!-- markdownlint-disable MD025 -->

# H5-005 · Agent 会话范围

> 本文件按照 `docs/_templates/implementation-scope.template.md` 编写，用于拆分工程单元，不可直接交给 `h5-coding-executor`。`status` / `reviewers` 由 Owner 人工维护。

## 1. 目标

以后端 MAF AG-UI 为第一优先级，实现 UI-005 的真实会话、消息流和官方 TypeScript AG-UI 客户端，并使用 Ant Design X 渲染协议事件；不把原型 mock 回放逻辑带入产品。

## 2. 上游依据

- `docs/01-requirements/requirements.md` REQ-010、REQ-018、NFR-003、NFR-005。
- `docs/01-requirements/ui-spec.md` §5 UI-005。
- `docs/01-requirements/acceptance-criteria.md` AC-036、AC-051、AC-060～064、AC-079、AC-084、AC-089。
- `docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md`。
- `docs/03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md`。

## 3. 当前基线

- **已有**：单 Agent 内存消息列表和 main process Chat Completions SSE 文本增量。
- **已有**：WebApi 通过 MAF `MapAGUI("/agent/{agentId}", agent)` 挂载 AG-UI。
- **缺失**：后端 AG-UI 契约闭环测试、状态兜底端点、服务端会话历史、TypeScript SDK、工具/Activity、取消和恢复。
- **偏差**：消息不跨设备，Renderer 数据模型只支持纯文本。
- **偏差**：真实后端路由与 ADR-012 的 `/api/runs` 描述不一致，必须先裁定并回写设计，不能由前端猜端点。

## 4. 范围

- UI-005 的会话列表、消息历史、流式事件、输入区、错误、重试和锁屏恢复。

## 5. 不做范围

- 多模态三条链路归 H5-010；Trace 详情归 H5-007。

## 6. 建议工程单元

| 子任务 | 单一交付目标 | 前置依赖 | 主要验证 |
| --- | --- | --- | --- |
| H5-005-A | 后端 MAF AG-UI 契约与集成测试 | ADR-012 路由裁定 | 真实 SSE 事件、鉴权、错误、心跳、取消、状态兜底 |
| H5-005-B | 最新稳定 TypeScript AG-UI SDK + Ant Design X/XMarkdown | H5-005-A、H5-002-A | SDK 集成、事件 reducer、Electron E2E |
| H5-005-C | 服务端会话与跨设备恢复 | H5-005-B、Conversations API | AC-084 |
| H5-005-D | 取消、错误、重试、断线和锁屏恢复 | H5-005-C | AC-079、AC-089 |

每个子任务执行前必须根据 `implementation-task-brief.template.md` 创建独立 `ai-task-brief.md`。

## 7. 契约与设计缺口

- 明确 AG-UI 端点输入、认证方式、事件集合和取消语义。
- 裁定并消除 ADR-012 `/api/runs` 与当前 `MapAGUI("/agent/{agentId}")` 的路由/状态兜底漂移。
- 补齐 Conversations REST API；当前 WebApi 只有协议端点，没有产品会话 Controller。
- 明确锁定期间 main process 如何继续持有流并把结果缓冲给 Renderer。
- 前端使用官方 npm 包 `@ag-ui/client` 和 `@ag-ui/core`；截至 2026-07-15 最新稳定版均为 `0.0.57`，执行时必须重新查询。
- UI 使用最新稳定 `@ant-design/x` 与 `@ant-design/x-markdown`；截至 2026-07-15 均为 `2.8.0`，执行时必须重新查询。

## 8. 风险与待确认项

以下原型代码不可复用到产品运行逻辑：

- 硬编码触发词和 mock 回复。
- `setTimeout` 时间线。
- 手写 SSE 编解码演示器和静态种子历史。

原型中的气泡、Sender、Markdown 和 Activity 视觉组件可以迁移，但数据必须来自真实协议事件。

- 不允许把当前 Chat Completions `choices[].delta.content` 解析器扩展成正式状态机；它只能作为被 AG-UI 替换前的现状基线。

## 9. 功能域完成定义

- 后端 AG-UI 契约先通过真实集成测试；前端使用官方 TypeScript SDK 消费同一契约，以 Ant Design X 渲染消息、工具和 Activity；会话来自服务端并支持跨设备恢复，锁屏和错误路径不丢结果。
