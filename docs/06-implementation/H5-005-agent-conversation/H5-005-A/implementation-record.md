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
updated: 2026-07-20
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

- **结论**：主体实现与完整测试矩阵均已完成。
- **代码基线**：`559de4d`。
- **记录日期**：2026-07-20。

## 2. 上游依据

- `docs/04-detailed-design/Inkwell.Core/HD-017-Inkwell.Core.Conversations.md` §0 当前契约。
- `docs/04-detailed-design/Inkwell.WebApi/HD-021-Inkwell.WebApi-authentication-and-agent-routing.md`。
- `docs/01-requirements/requirements.md` REQ-010、REQ-018、NFR-005。

## 3. 已实现内容

| 路径 / 符号 | 当前职责 | 对应需求 |
| --- | --- | --- |
| `Inkwell.Abstractions/Persistence/Conversations`、`Agent/Conversations` | Conversation、消息两模型和 Repository / Service 端口 | REQ-010、NFR-005 |
| `Inkwell.Core/AgentRuntime/AgentConversationService` | 创建、列表、历史、删除、清空、运行编排和消息幂等提交 | REQ-010 |
| `Inkwell.Core/AgentRuntime/ChatHistory` | 从消息仓储恢复跨轮历史并提交本轮消息 | REQ-010、NFR-005 |
| `Inkwell.Persistence.EFCore` | Conversation、Message 两表 Entity、Mapping、Configuration 和 Repository | NFR-005 |
| `Inkwell.WebApi/Controllers/AgentConversationsController` | Conversation 产品 REST | REQ-010、REQ-018 |
| `Inkwell.Persistence.EFCore.{SqlServer,Postgres}/Migrations` | 双 Provider `RemoveAgentSessionState` Migration | NFR-005 |

## 4. 已验证证据

| 验证项 | 命令或测试 | 结果 | 日期 |
| --- | --- | --- | --- |
| Solution build | `dotnet build Inkwell.slnx --no-restore` | 通过 | 2026-07-20 |
| Full test matrix | `dotnet test Inkwell.slnx --no-build` | 93/93 通过 | 2026-07-20 |
| Conversation external history | `AgentConversationServiceRunTests` | 两轮运行从消息仓储恢复历史，且不调用 Session 序列化 | 2026-07-20 |

## 5. 待补验证与实现缺口

| 缺口 | 关联 AC / 风险 | 后续任务 |
| --- | --- | --- |
| SQL Server Repository 行为覆盖需要与 PostgreSQL 对称核验 | Provider 一致性 | H5-005-A-V |

## 6. 已知偏差

- 原 H5-005-A brief 要求 Run Lease 与 fencing；该设计已被 HD-017 当前契约移除，实际实现不包含这些字段和操作。
- 正式 Conversation 每轮新建 MAF Session；跨轮连续性由 `AgentChatMessage` 唯一事实源恢复，不持久化 Session checkpoint。

## 7. 后续任务

- H5-005-A-V：补齐 SQL Server 与 PostgreSQL 对称的 Repository contract 覆盖。
- H5-005-D：完成取消、错误、重试、断线和锁屏恢复。

## 8. 维护规则

- 新验证完成后更新 §4 和 §5。
- 行为发生变化时直接更新当前状态；历史由 git 保留。
- 不在本文件代签 `status` / `reviewers`。
