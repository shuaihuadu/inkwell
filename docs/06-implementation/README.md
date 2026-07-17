---
id: H5-INDEX
title: H5 实施任务索引
stage: H5
document_type: index
status: draft
authors:
	- name: GitHub Copilot
		role: agent
reviewers: []
created: 2026-07-15
updated: 2026-07-17
upstream: []
downstream: []
---

<!-- markdownlint-disable MD025 -->

# H5 实施任务索引

本目录记录 H5 编码阶段的实施路线、已实现功能记录和可交给 `h5-coding-executor` 的任务简报。

## 文档类型

- `implementation-record.md`：代码已经存在时，记录真实实现、验证结果和剩余缺口；不是待执行任务。
- `scope.md`：功能域的实施边界与拆分建议；尚不能直接交给编码 Agent。
- `ai-task-brief.md`：范围、允许修改文件、测试和验收命令已锁定；可直接交给 `h5-coding-executor`。

对应模板：

- `docs/_templates/implementation-record.template.md`
- `docs/_templates/implementation-scope.template.md`
- `docs/_templates/implementation-task-brief.template.md`

不得仅凭目录编号判断完成状态，以本索引的“状态”列和各文档正文为准。

## Desktop 实施单元

| 编号 | 实施单元 | 主要范围 | 当前状态 | 执行文档 |
| --- | --- | --- | --- | --- |
| H5-001 | 登录、会话与锁定 | UI-001、UI-002、会话恢复、登出、自动锁定 | 已实现，待补验证 | [实施记录](H5-001-authentication-and-lock/implementation-record.md) |
| H5-002 | 公共外壳与全局体验 | 依赖升级、AppShell、权限导航和主题已完成；继续网络状态与全局错误 | H5-002-A/B 已实现，H5-002-C 待设计 | [范围说明](H5-002-app-shell/scope.md) |
| H5-003 | Agent 空间与基础管理 | UI-003、我的/团队共享、搜索、筛选、删除、共享、点击分流 | 待起草任务简报 | [范围说明](H5-003-agent-space/scope.md) |
| H5-004 | Agent 设计与配置 | UI-004、基础属性、模型参数、工具、Skills、知识库、记忆、草稿/发布 | 待设计核验 | [范围说明](H5-004-agent-design/scope.md) |
| H5-005 | Agent 会话 | 会话数据与 REST 优先；随后接入 MAF AG-UI、TypeScript SDK、Ant Design X 与 Activity | 待设计核验 | [范围说明](H5-005-agent-conversation/scope.md) |
| H5-006 | 版本管理 | UI-008、版本列表、diff、回滚 | 待起草任务简报 | 路线图定义 |
| H5-007 | 调试与评测 | UI-007、Trace、评测集、回放 | H3 未完成 | 路线图定义 |
| H5-008 | Admin 管理 | UI-009、账号解封、撤销他人共享 | H3 未完成 | 路线图定义 |
| H5-009 | API Key 与外部协议 | API Key 创建/撤销/绑定、四协议调用验证 | H3 未完成 | 路线图定义 |
| H5-010 | 多模态输入 | 图片、语音转写、文档输入、能力前置校验 | H3 未完成 | 路线图定义 |
| H5-011 | 桌面横切质量 | 离线与错误规约、可访问性、性能、Windows/macOS 验证 | 持续实施 | 路线图定义 |
| H5-012 | 桌面打包与更新 | 安装包、签名、公证、自动更新、发布通道 | 尚未规划 | 路线图定义 |

总体依赖、实施顺序和各单元完成定义见 [Desktop 实施路线图](desktop-implementation-roadmap.md)。

## 执行规则

1. `h5-coding-executor` 每次只接收一个 `ai-task-brief.md`。
2. `scope.md` 必须结合真实代码和上游设计收敛为任务简报后才能执行。
3. 已实现功能先写实施记录并运行验证，不为追求文档形式重写已有代码。
4. 若一个功能域同时涉及前端、IPC 和后端，应按可独立验收的垂直切片继续拆成 `H5-NNN-A/B`，不得扩大单次任务。
