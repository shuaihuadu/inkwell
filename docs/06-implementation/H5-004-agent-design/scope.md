---
id: H5-004
title: Agent 设计与配置 · 实施范围
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
	- REQ-003
	- REQ-004
	- REQ-005
	- REQ-006
	- REQ-007
	- REQ-008
	- REQ-009
	- REQ-010
	- REQ-015
downstream:
	- H5-004-A
	- H5-004-B
	- H5-004-C
	- H5-004-D
	- H5-004-E
---

<!-- markdownlint-disable MD025 -->

# H5-004 · Agent 设计与配置范围

> 本文件按照 `docs/_templates/implementation-scope.template.md` 编写，用于拆分工程单元，不可直接交给 `h5-coding-executor`。`status` / `reviewers` 由 Owner 人工维护。

## 1. 目标

实现 UI-004，将原型的 Agent 设计体验接入真实 Agent、模型、工具、Skill、知识库、记忆和版本服务。

## 2. 上游依据

- `docs/01-requirements/requirements.md` REQ-003～010、REQ-015。
- `docs/01-requirements/ui-spec.md` §4 UI-004。
- `docs/01-requirements/acceptance-criteria.md` AC-014～050、AC-056、AC-096。
- `docs/04-detailed-design/Inkwell.Core/HD-015-Inkwell.Core.Agents.md`。

## 3. 当前基线

- **已有**：创建 Agent Modal、模型列表、Agent CRUD 和版本 REST 端点。
- **缺失**：独立配置页、多区段数据加载、草稿/发布状态和绝大多数配置 API。
- **偏差**：当前创建流程立即发布。

## 4. 范围

- UI-004 的基础信息、Instructions、模型参数、工具、Skills、知识库、记忆和提交状态。

## 5. 不做范围

- 版本 diff/回滚归 H5-006；独立会话页归 H5-005；Trace 归 H5-007。

## 6. 建议工程单元

| 子任务 | 单一交付目标 | 前置依赖 | 主要验证 |
| --- | --- | --- | --- |
| H5-004-A | 基础属性、头像、Instructions、模型参数 | Agent/Models API | AC-014～024 |
| H5-004-B | 工具与 Skills 绑定及校验 | Tools/Skills API | AC-025～032 |
| H5-004-C | 知识库与长期记忆配置 | KB/Memory API | AC-033～037 |
| H5-004-D | 草稿、发布、权限和修改状态 | Version API | AC-056、AC-096 |
| H5-004-E | 配置页内嵌对话面板 | H5-005-C | 共享状态机回归 |

每个子任务执行前必须根据 `implementation-task-brief.template.md` 创建独立 `ai-task-brief.md`。

## 7. 契约与设计缺口

- 确认每个区段已有对应后端服务和 HTTP API；缺失时先完成 H3/API 设计和后端切片。
- 明确 Agent draft snapshot 的稳定 JSON 契约，避免 Renderer 自行拼装不兼容快照。
- 明确头像、知识库文件和 Skill 上传的文件 API。

## 8. 风险与待确认项

- 当前范围横跨多个后端业务模块，任何子任务缺少稳定 API 时必须先补 H3/API 设计。

## 9. 功能域完成定义

- 草稿保存不产生版本，不影响当前发布版本和在途对话。
- 发布生成新版本并清除“有未发布的修改”。
- 非 Owner 只读且看不到写操作。
- 所有选项来自后端，不硬编码模型、工具或 Skill。
