---
id: H5-008-A-RECORD
title: Admin 用户管理 · 实施记录
stage: H5
document_type: implementation-record
status: draft
implementation_state: implemented
authors:
  - name: GitHub Copilot
    role: agent
reviewers: []
created: 2026-07-18
updated: 2026-07-18
upstream:
  - REQ-017
  - UI-009
  - HD-014
tests: []
downstream: []
---

# H5-008-A Admin 用户管理实施记录

> 本文件只记录仓库中可核实的当前实现和验证证据。`status` / `reviewers` 由 Owner 人工维护。

## 1. 实施范围

本切片实现 UI-009 的账号列表与解封，不包含撤销他人共享、审计日志、账号创建、删除、密码修改、角色修改或 RBAC。

## 2. 已实现内容

| 路径 / 符号 | 当前职责 |
| --- | --- |
| `AuthController.ListAccountsAsync` | Super 用户查询部署内账号，可按锁定状态筛选 |
| `AuthController.UnlockAccountAsync` | Super 用户以当前身份解封目标账号 |
| `electron/main.ts` | 在 main 进程再次校验 Super 身份并代理账号查询、解封请求 |
| `features/users/user-management.tsx` | 用户搜索、状态/角色筛选、分页、解封二次确认和状态刷新 |
| `shared/components/data-list-page.tsx` | 用户列表与模型列表共用的查询、刷新、空态、表格和分页结构 |
| `workspace-shell.tsx` | 仅向 Super 用户展示用户管理入口 |

## 3. 验证证据

| 验证项 | 命令或范围 | 结果 |
| --- | --- | --- |
| WebApi 完整回归 | `dotnet test tests/Inkwell.WebApi.Tests/Inkwell.WebApi.Tests.csproj --no-restore` | 18/18 通过 |
| Solution build | `dotnet build Inkwell.slnx --no-restore` | 通过 |
| Desktop production build | `npm --prefix src/app/desktop run build` | 通过 |
| Desktop lint | `npm --prefix src/app/desktop run lint` | 通过 |
| Electron E2E | `npm --prefix src/app/desktop run test:e2e` | 3/3 通过；覆盖 Super 列表/解封和 Member 导航不可见 |
| Patch hygiene | `git diff --check` | 通过 |

## 4. 剩余范围

- H5-008 的“撤销他人共享”尚未实施，需按 Agent 共享契约另行形成切片。
- 当前没有独立 H4 TC；本切片以 UI-009、HD-014 和现有 Controller/Electron E2E 为验证依据。
