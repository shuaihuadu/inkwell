---
id: H5-002-B-RECORD
title: Desktop 公共 AppShell 与权限导航 · 实施记录
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
  - NFR-001
  - NFR-003
  - ADR-001
  - H5-002-A
tests:
  - AC-007
downstream:
  - H5-002-C
  - H5-003-A
---

<!-- markdownlint-disable MD025 -->

# H5-002-B · Desktop 公共 AppShell 与权限导航实施记录

## 1. 实施状态

- **结论**：已实现并完成验收。
- **代码基线**：当前工作区。
- **记录日期**：2026-07-17。

## 2. 上游依据

- `docs/01-requirements/ui-spec.md` §0.2。
- `docs/06-implementation/H5-002-app-shell/scope.md`。

## 3. 已实现内容

| 路径 / 符号 | 当前职责 | 对应需求 |
| --- | --- | --- |
| `features/shell/workspace-shell.tsx` | Header、关于弹层、外观设置、用户菜单、分组导航、Super 权限过滤、占位页面和登出 | UI 公共外壳 |
| `features/shell/appearance-store.ts` | 持久化亮色、暗色、跟随系统和三套主题色 | UI 公共外壳 |
| `features/shell/desktop-theme-provider.tsx` / `themes.ts` | 根据外观与主题色驱动 Ant Design 根主题和壳层主题变量 | UI 公共外壳 |
| `electron/main.ts` / `electron/preload.ts` | 通过 typed preload 暴露真实应用版本、构建号和提交信息 | UI 公共外壳 |
| `features/auth/lock-page.tsx` | 原型对齐的主题化锁屏和解锁入口 | NFR-003 |
| `features/agent-library/agent-workspace.tsx` | 只保留 Agent 查询、创建和聊天业务内容 | REQ-002 |
| `app-shell.tsx` | 认证状态分流并组合公共壳层与业务内容 | NFR-003 |
| `tests/login.spec.ts` | Super/普通用户导航和占位入口 Electron E2E | AC-007 |

## 4. 已验证证据

| 验证项 | 命令或测试 | 结果 | 日期 |
| --- | --- | --- | --- |
| Desktop 构建 | `npm --prefix src/app/desktop run build` | 通过 | 2026-07-17 |
| ESLint | `npm --prefix src/app/desktop run lint` | 通过 | 2026-07-17 |
| Vitest 入口 | `npm --prefix src/app/desktop run test` | 通过；当前无单测 | 2026-07-17 |
| Electron 登录、Header、主题、锁屏与导航 E2E | `npm --prefix src/app/desktop run test:e2e` | 3 passed | 2026-07-17 |
| 编辑器诊断 | `get_errors` | 无错误 | 2026-07-17 |

## 5. 待补验证与实现缺口

| 缺口 | 关联 AC / 风险 | 后续任务 |
| --- | --- | --- |
| 后台服务状态仍为静态基线 | EX-001 | H5-002-C |
| 全局 401、429、5xx 错误条尚未统一 | EX-001 | H5-002-C |

## 6. 已知偏差

- 最新原型源码已取消整体导航收起，产品实现同样保持固定 200px，只允许分组展开/折叠。
- 主窗口失焦不再立即锁定，而是按五分钟无活动计时；操作系统锁屏事件仍立即进入 UI-002。

## 7. 后续任务

- H5-002-C：在稳定 IPC 契约上实现连接状态和全局错误映射。
- H5-003-A：在公共壳层内实现 Agent 卡片空间。

## 8. 维护规则

- 新验证完成后更新 §4 和 §5。
- 行为发生变化时直接更新当前状态；历史由 git 保留。
- 不在本文件代签 `status` / `reviewers`。
