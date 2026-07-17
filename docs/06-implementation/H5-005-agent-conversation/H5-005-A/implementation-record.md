---
id: H5-005-A-RECORD
title: Agent Conversation 数据与 REST 基座 · 实施记录
stage: H5
document_type: implementation-record
status: draft
implementation_state: implemented
authors:
  - name: GitHub Copilot
    role: agent
reviewers: []
created: 2026-07-17
updated: 2026-07-17
upstream:
  - REQ-010
  - REQ-018
  - NFR-005
  - HD-017
  - HD-021
tests:
  - AC-036
  - AC-051
  - AC-060
  - AC-061
  - AC-062
  - AC-063
  - AC-064
  - AC-084
downstream:
  - H5-005-B
---

<!-- markdownlint-disable MD025 -->

# H5-005-A · Agent Conversation 数据与 REST 基座实施记录

> 本文件只记录仓库中可核实的当前实现和验证证据。`status` / `reviewers` 由 Owner 人工维护。

## 1. 实施状态

- **结论**：主体实现已完成，当前会话尚未重跑完整测试矩阵。
- **代码基线**：`9b7e03d` 及当前工作区；双 Provider Migration 当前为未跟踪文件。
- **记录日期**：2026-07-17。

## 2. 上游依据

- `docs/04-detailed-design/Inkwell.Core/HD-017-Inkwell.Core.Conversations.md` §0 当前契约。
- `docs/04-detailed-design/Inkwell.WebApi/HD-021-Inkwell.WebApi-authentication-and-agent-routing.md`。
- `docs/01-requirements/requirements.md` REQ-010、REQ-018、NFR-005。

## 3. 已实现内容

| 路径 / 符号 | 当前职责 | 对应需求 |
| --- | --- | --- |
| `Inkwell.Abstractions/Persistence/Conversations` | Conversation、消息、SessionState 三模型和 Repository 端口 | REQ-010、NFR-005 |
| `Inkwell.Core/Conversations/AgentConversationService` | 创建、列表、历史、删除、清空、消息幂等提交和状态保存 | REQ-010 |
| `Inkwell.Persistence.EFCore` | 三表 Entity、Mapping、Configuration 和 Repository | NFR-005 |
| `Inkwell.WebApi/Controllers/AgentConversationsController` | Conversation 产品 REST | REQ-010、REQ-018 |
| `Inkwell.Persistence.EFCore.{SqlServer,Postgres}/Migrations` | 双 Provider Initial Migration | NFR-005 |

## 4. 已验证证据

| 验证项 | 命令或测试 | 结果 | 日期 |
| --- | --- | --- | --- |
| Conversation Model | `AgentConversationModelTests` | 测试代码已存在，本记录创建时未重跑 | 2026-07-17 |
| Conversation Service | `AgentConversationServiceTests` | 测试代码已存在，本记录创建时未重跑 | 2026-07-17 |
| PostgreSQL Repository | `AgentConversationPostgresRepositoryTests` | 测试代码已存在，本记录创建时未重跑 | 2026-07-17 |

## 5. 待补验证与实现缺口

| 缺口 | 关联 AC / 风险 | 后续任务 |
| --- | --- | --- |
| Abstractions、Core、WebApi、Provider 全矩阵未在本记录创建时重跑 | AC-036、AC-051、AC-060～064、AC-084 | H5-005-A-V |
| SQL Server Repository 行为覆盖需要与 PostgreSQL 对称核验 | Provider 一致性 | H5-005-A-V |
| Migration 当前未纳入 git 跟踪，需由 Owner 决定后续提交范围 | 部署可重复性 | 独立提交审计 |

## 6. 已知偏差

- 原 H5-005-A brief 要求 Run Lease 与 fencing；该设计已被 HD-017 当前契约移除，实际实现不包含这些字段和操作。
- 实际 Agent Session 的持久化和恢复仍暂缓，归后续 H5-005-B 设计核验。

## 7. 后续任务

- H5-005-A-V：运行并补齐 Conversation 相关测试矩阵，更新本记录验证证据。
- H5-005-B：基于现有 `MapAGUI` 与 `InkwellChatHistoryProvider` 验证真实 AG-UI wire contract。

## 8. 维护规则

- 新验证完成后更新 §4 和 §5。
- 行为发生变化时直接更新当前状态；历史由 git 保留。
- 不在本文件代签 `status` / `reviewers`。
