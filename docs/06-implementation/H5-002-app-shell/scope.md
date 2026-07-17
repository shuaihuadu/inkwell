---
id: H5-002
title: 公共外壳与全局体验 · 实施范围
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
	- NFR-001
	- NFR-003
	- ADR-001
downstream:
	- H5-002-A
	- H5-002-B
	- H5-002-C
---

<!-- markdownlint-disable MD025 -->

# H5-002 · 公共外壳与全局体验范围

> 本文件按照 `docs/_templates/implementation-scope.template.md` 编写，用于拆分工程单元，不可直接交给 `h5-coding-executor`。`status` / `reviewers` 由 Owner 人工维护。

## 1. 目标

实现认证后的公共 AppShell，不承载 Agent 列表、配置或会话的业务逻辑。

## 2. 上游依据

- `docs/01-requirements/ui-spec.md` §0.2 公共外壳与全局元素。
- `docs/01-requirements/requirements.md` NFR-001、NFR-003、§13 第 29、31 条。
- `docs/03-architecture/adr/ADR-001-client-runtime-electron-react.md`。

## 3. 当前基线

- **已有**：Ant Design 6 依赖基线、公共 AppShell、三组两级导航、Super 权限过滤、占位页面、关于弹层，以及亮色/暗色/跟随系统和三套主题色设置。
- **缺失**：真实统一网络状态和全局错误映射。
- **当前偏差**：Header 的后台服务状态仍为静态正常基线，尚未连接重连中与异常状态。

## 4. 范围

- 56px 顶栏、三组两级导航和主内容容器。
- 工作区：Agent 空间。
- 资源中心：工具管理、Skills 管理、模型管理三个“即将上线”占位入口。
- 系统管理：Admin，仅 `isSuper=true` 可见。
- 用户菜单、登出、网络状态和全局错误条。
- 个人设置提供亮色、暗色、跟随系统，以及曜石紫、朱砂橙、碧海青三套主题色；设置在本机持久化。
- 审计 Desktop 全部 npm 依赖，使用架构约束内的最新稳定版。
- 升级至最新稳定的 Ant Design、`@ant-design/icons`，并完成登录/锁定页面的最小兼容调整。
- 安装最新稳定的 Ant Design X 与 XMarkdown，但其聊天组件接线归 H5-005-C。

## 5. 不做范围

- Agent 卡片、搜索、删除、共享归 H5-003。
- Agent 配置、版本、会话和 Admin 具体功能归各自实施单元。
- Design Lab 的 mock 网络切换器不进入产品。

## 6. 建议工程单元

当前进度：H5-002-A/B 已由 `726ebd6` 实现并验证；H5-002-C 待起草任务简报。

| 子任务 | 单一交付目标 | 前置依赖 | 主要验证 |
| --- | --- | --- | --- |
| H5-002-A | 全量依赖审计，升级 Ant Design 6，并安装 Ant Design X/XMarkdown | H5-001 当前实现 | clean install、build、lint、登录 E2E |
| H5-002-B | AppShell、权限导航和占位入口 | H5-002-A | 导航权限 Electron E2E |
| H5-002-C | 网络状态与全局错误映射 | H5-002-B | 401、429、5xx、离线 E2E |

每个子任务执行前必须根据 `implementation-task-brief.template.md` 创建独立 `ai-task-brief.md`。

## 7. 契约与设计缺口

- 需要明确 Renderer 可消费的统一连接状态和 API 错误结构。
- 当前架构锁定 Vite 6、TypeScript 5.x；截至 2026-07-15 npm 最新稳定版为 Vite 8.1.4、TypeScript 7.0.2。是否跨主版本升级必须先走 H2，不属于 H5-002-A。

## 8. 风险与待确认项

- Ant Design 6 的登录、工作区和锁屏回归已通过 macOS Electron E2E；Windows 11 验证仍归 H5-011。
- “使用最新版本”必须在每次执行时重新查询 npm；本文记录的版本不能作为永久最新值。

## 9. 功能域完成定义

- 所有认证后页面共享统一 AppShell、权限导航、网络状态和错误映射。
