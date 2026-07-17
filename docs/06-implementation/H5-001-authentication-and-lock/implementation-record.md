---
id: H5-001
title: 登录、会话与锁定 · 实施记录
stage: H5
document_type: implementation-record
status: draft
implementation_state: implemented
authors:
	- name: GitHub Copilot
		role: agent
reviewers: []
created: 2026-07-15
updated: 2026-07-17
upstream:
	- REQ-001
	- NFR-003
	- ADR-011
tests:
	- AC-001
	- AC-004
	- AC-005
	- AC-076
	- AC-077
	- AC-078
	- AC-079
	- AC-080
downstream:
	- H5-001-A
	- H5-001-B
---

<!-- markdownlint-disable MD025 -->

# H5-001 · 登录、会话与锁定实施记录

> 本文件按照 `docs/_templates/implementation-record.template.md` 编写，只记录当前代码可核实的事实。`status` / `reviewers` 由 Owner 人工维护。

## 1. 实施状态

- **结论**：主体功能已实现；锁定、主题化锁屏和成功解锁已有 Electron E2E，恢复边界与在途任务验证仍不完整。
- **代码基线**：`726ebd6`。
- **记录日期**：2026-07-17。

## 2. 上游依据

- `docs/01-requirements/requirements.md`：REQ-001、NFR-003。
- `docs/01-requirements/ui-spec.md`：UI-001、UI-002。
- `docs/01-requirements/acceptance-criteria.md`：AC-001、AC-004、AC-005、AC-076～080。
- `docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md`。

## 3. 已实现内容

- `src/app/desktop/src/features/auth/login-page.tsx`：登录表单、离线状态和登录错误提示。
- `src/app/desktop/src/features/auth/auth-store.ts`：Renderer 认证快照状态。
- `src/app/desktop/src/features/auth/lock-page.tsx`：原型对齐的主题化锁屏、密码解锁、切换账号和登出入口。
- `src/app/desktop/src/app-shell.tsx`：认证恢复、活动上报、登录/锁定/工作区切换。
- `src/app/desktop/electron/main.ts`：登录、会话恢复、`safeStorage` Token 持久化、登出、五分钟空闲锁定、窗口失焦后重新计时和系统锁屏立即锁定。
- `src/app/desktop/electron/preload.ts`：认证相关 typed IPC bridge。
- `src/app/desktop/tests/login.spec.ts`：登录视觉基线、错误提示和登录后进入工作区的 Electron E2E。

## 4. 已验证证据

| 验证项 | 命令或测试 | 结果 | 日期 |
| --- | --- | --- | --- |
| 登录页视觉、登录错误、成功进入工作区 | `npm --prefix src/app/desktop run test:e2e` | 3 passed | 2026-07-17 |
| 窗口失焦不立即锁定、系统锁屏、锁屏视觉和成功解锁 | `npm --prefix src/app/desktop run test:e2e` | 3 passed | 2026-07-17 |

## 5. 待补验证与实现缺口

| 缺口 | 关联 AC / 风险 | 后续任务 |
| --- | --- | --- |
| 24 小时内重启恢复与过期边界没有独立 E2E | AC-004 | H5-001-A |
| 五分钟计时边界、解锁失败、账号锁定和离线解锁缺少独立 Electron E2E | AC-076～080 | H5-001-A |
| 锁屏期间在途流、上传和转写的结果累积尚未验证 | AC-079 / OQ-017 | H5-001-B |
| Windows 11 与 macOS 12+ Apple Silicon 矩阵尚未执行 | NFR-002 | H5-011 |

## 6. 已知偏差

- 无；主窗口失焦已改为重新开始五分钟无活动计时，操作系统锁屏事件仍立即进入 UI-002。

## 7. 后续任务

- 先新增 H5-001-A 测试任务，只补认证与锁定 E2E，不重写实现。
- H5-005-D 和 H5-010 实施后，再补 H5-001-B 在途任务锁屏恢复测试。

## 8. 维护规则

- 新验证完成后追加 §4 并更新 §5，不写编年史式叙事。
- 行为发生变化时直接更新当前状态；历史由 git 和评审记录保留。
- 不在本文件代签 `status` / `reviewers`。
