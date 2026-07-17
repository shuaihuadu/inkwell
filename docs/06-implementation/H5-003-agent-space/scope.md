---
id: H5-003
title: Agent 空间与基础管理 · 实施范围
stage: H5
document_type: scope
status: draft
authors:
	- name: GitHub Copilot
		role: agent
reviewers: []
created: 2026-07-15
updated: 2026-07-17
upstream:
	- REQ-002
downstream:
	- H5-003-A
	- H5-003-B
	- H5-003-C
---

<!-- markdownlint-disable MD025 -->

# H5-003 · Agent 空间与基础管理范围

> 本文件按照 `docs/_templates/implementation-scope.template.md` 编写，用于拆分工程单元，不可直接交给 `h5-coding-executor`。`status` / `reviewers` 由 Owner 人工维护。

## 1. 目标

实现 UI-003 与 REQ-002，使用真实 Agent API，不承载配置表单或会话内部实现。

## 2. 上游依据

- `docs/01-requirements/requirements.md` REQ-002、§13 第 30～32 条。
- `docs/01-requirements/ui-spec.md` §3 UI-003。
- `docs/01-requirements/acceptance-criteria.md` AC-007～013。

## 3. 当前基线

- **已有**：我的 Agent 列表、名称搜索、模型查询和 Agent 选择。
- **缺失**：团队共享、卡片网格、最近使用时间、Owner 展示名、删除、共享和页面分流。
- **偏差**：当前列表项统一在同页打开 ChatPanel，新建后会被 main process 立即发布。

## 4. 范围

- “我的 / 团队共享”tab，默认进入“我的”。
- 响应式卡片网格、头像占位、描述、Owner、版本和更新时间。
- 搜索、刷新、加载、空态、错误和重试。
- Owner 的编辑、删除、共享和撤销共享操作。
- 已发布 Agent 点击进入 UI-005；纯草稿点击进入 UI-004。
- “新建 Agent”进入 UI-004 空白草稿，不再创建后立即发布。

## 5. 不做范围

- Agent 配置字段和保存逻辑归 H5-004。
- 消息流、会话历史或 AG-UI 归 H5-005。
- Admin 撤销他人共享归 H5-008。

## 6. 建议工程单元

| 子任务 | 单一交付目标 | 前置依赖 | 主要验证 |
| --- | --- | --- | --- |
| H5-003-A | 只读列表、tab、卡片和搜索 | H5-002-B + 列表展示契约 | AC-007、AC-013 |
| H5-003-B | 删除、共享、撤销共享及权限处理 | H5-003-A | AC-009～011 |
| H5-003-C | 新建、编辑、对话的页面分流与返回 | H5-004/H5-005 页面入口 | AC-008、点击分流 |

每个子任务执行前必须根据 `implementation-task-brief.template.md` 创建独立 `ai-task-brief.md`。

## 7. 契约与设计缺口

- `AgentListItem` 只有 `OwnerUserId` 和 `UpdatedTime`，无法满足 AC-013 的 Owner 展示名与查看者维度最近使用时间；不得在 Desktop 用 GUID 或更新时间替代。
- reviewed HD-017 已锁定查看者维度语义，但当前 `IAgentConversationService` / `IAgentConversationRepository` 尚无批量最近活动查询；需要先完成后端契约、EF 查询、WebApi 组合和测试。
- H5-003-A 在上述列表展示契约完成前为 **blocked**；可先起草前置后端任务，但不得直接实现失真的卡片字段。
- 当前 `electron/main.ts` 的创建后自动发布行为应在 H5-004-D 删除；H5-003 只负责入口，不提交空白 Agent。

## 8. 风险与待确认项

- H5-003-C 依赖 UI-004/UI-005 的真实导航入口，不能用永久占位页伪装完成。

## 9. 功能域完成定义

- 用户可浏览和管理授权范围内的 Agent，并按发布状态进入正确页面。
