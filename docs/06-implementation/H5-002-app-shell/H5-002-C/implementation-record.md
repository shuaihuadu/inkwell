---
id: H5-002-C-RECORD
title: Desktop 后台连接状态与全局错误处理 · 实施记录
stage: H5
document_type: implementation-record
status: draft
implementation_state: implemented
authors:
  - name: GitHub Copilot
    role: agent
reviewers: []
created: 2026-08-25
updated: 2026-08-25
upstream:
  - NFR-001
  - EX-001
  - H5-002-B
tests:
  - AC-071
  - AC-072
downstream: []
---

<!-- markdownlint-disable MD025 -->

# H5-002-C · Desktop 后台连接状态与全局错误处理实施记录

## 1. 实施状态

- **结论**：已实现并完成自动化验收，等待 Owner 评审。
- **代码分支**：`feature/global-network-errors`。
- **记录日期**：2026-08-25。

## 2. 上游依据

- `docs/01-requirements/requirements.md` NFR-001、EX-001。
- `docs/01-requirements/ui-spec.md` §0.2、§10.1、§11。
- `docs/01-requirements/acceptance-criteria.md` AC-071、AC-072。
- `docs/06-implementation/H5-002-app-shell/H5-002-C/ai-task-brief.md`。
- `prototypes/inkwell-visual-design/src/pages/AppShellExplorer.tsx`。

## 3. 已实现内容

| 路径 / 符号                                           | 当前职责                                                               | 对应要求          |
| ----------------------------------------------------- | ---------------------------------------------------------------------- | ----------------- |
| `electron/network-status.ts`                          | 三态连接转换、失败阈值、429/5xx 安全分类和 `Retry-After` 解析          | EX-001            |
| `electron/main.ts`                                    | 单例 `/healthz` 探测、状态广播、非在线写请求门禁、普通 API 错误映射    | AC-071、AC-072    |
| `electron/preload.ts` / `shared/network/contracts.ts` | 通过 typed preload 暴露连接快照和结构化全局错误事件                    | Electron 安全边界 |
| `features/shell/network-store.ts`                     | Renderer 连接状态与全局错误状态                                        | EX-001            |
| `features/shell/global-api-alert.tsx`                 | 登录、锁屏和工作区共享的持久全局错误条                                 | AC-071、AC-072    |
| `features/shell/workspace-shell.tsx`                  | 原型对齐的 online、reconnecting、offline 顶栏徽标                      | UI §0.2           |
| `features/auth/login-page.tsx`                        | 后台未在线时禁用登录提交                                               | AC-071            |
| `tests/login.spec.ts`                                 | 真实 Electron IPC/request 路径的离线、恢复、写门禁、429、5xx、401 验证 | AC-071、AC-072    |

## 4. 已验证证据

| 验证项            | 命令或测试                                  | 结果                                       | 日期       |
| ----------------- | ------------------------------------------- | ------------------------------------------ | ---------- |
| Desktop 单元测试  | `npm --prefix src/app/desktop run test`     | 7 files、28 tests passed                   | 2026-08-25 |
| ESLint            | `npm --prefix src/app/desktop run lint`     | 通过，0 error / 0 warning                  | 2026-08-25 |
| Desktop 构建      | `npm --prefix src/app/desktop run build`    | TypeScript 与 Electron Vite 构建通过       | 2026-08-25 |
| Electron E2E      | `npm --prefix src/app/desktop run test:e2e` | 5 passed                                   | 2026-08-25 |
| .NET 解决方案构建 | `dotnet build Inkwell.slnx`                 | 23 projects succeeded，0 warning / 0 error | 2026-08-25 |

## 5. 行为边界

- 仅健康探测自动重试；业务写请求不排队、不缓存、不自动重放。
- 任意普通 API HTTP 响应均证明服务可达；只有 fetch 级失败和 `/healthz` 非成功响应推进 reconnecting/offline。
- 登录、解锁和聊天继续保留原有局部错误语义；普通 429/5xx 额外形成全局错误。
- 5xx 全局提示使用固定本地化文案，不显示后端响应正文。

## 6. 已知偏差

无。任务简报中误写的样式入口 `styles.css` 已按仓库实际文件修正为 `index.css`，未扩大产品改动范围。

## 7. 残余风险

- `/healthz` 只表示 WebApi 可达和现有端点定义的健康范围，不代表所有下游依赖健康。
- reconnecting 使用两秒固定间隔，offline 使用五秒固定间隔；两者均有界，但当前不使用指数退避。
- 各业务页面仍可保留原有局部失败提示；主进程写门禁是不可绕过的最终保护。

## 8. 维护规则

- 行为或验证基线变化时直接更新本记录，历史由 git 保留。
- `status` / `reviewers` 由 Owner 人工维护，Agent 不代签。
