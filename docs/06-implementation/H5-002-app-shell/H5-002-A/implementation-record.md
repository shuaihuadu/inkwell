---
id: H5-002-A-RECORD
title: Desktop 前端依赖升级与兼容基线 · 实施记录
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
tests:
  - AC-001
  - AC-076
downstream:
  - H5-002-B
  - H5-005-C
---

<!-- markdownlint-disable MD025 -->

# H5-002-A · Desktop 前端依赖升级与兼容基线实施记录

## 1. 实施状态

- **结论**：已实现并完成验收。
- **代码基线**：`726ebd6`。
- **记录日期**：2026-07-17。

## 2. 上游依据

- `docs/03-architecture/adr/ADR-001-client-runtime-electron-react.md`。
- `docs/06-implementation/H5-002-app-shell/scope.md`。

## 3. 已实现内容

| 路径 / 符号 | 当前职责 | 对应需求 |
| --- | --- | --- |
| `src/app/desktop/package.json` / `package-lock.json` | Ant Design 6、Icons 6、Ant Design X 与 XMarkdown 依赖基线 | H5-002、H5-005 |
| `src/app/desktop/vite.config.ts` | 隔离 Vitest 与 Electron Playwright E2E，并允许当前空单测集 | H5-011 |

## 4. 已验证证据

| 验证项 | 命令或测试 | 结果 | 日期 |
| --- | --- | --- | --- |
| npm 依赖树与安全审计 | `npm install --save-exact ...` | 0 vulnerabilities，无 peer dependency 错误 | 2026-07-17 |
| Desktop 构建 | `npm --prefix src/app/desktop run build` | 通过 | 2026-07-17 |
| ESLint | `npm --prefix src/app/desktop run lint` | 通过 | 2026-07-17 |
| Vitest | `npm --prefix src/app/desktop run test` | 通过；当前无单测，不再误收集 E2E | 2026-07-17 |
| Electron 登录 E2E | `npm --prefix src/app/desktop run test:e2e` | 2 passed | 2026-07-17 |

## 5. 待补验证与实现缺口

| 缺口 | 关联 AC / 风险 | 后续任务 |
| --- | --- | --- |
| 当前没有 Renderer 单元测试 | H5-011 | 随新增状态逻辑按切片补充 |
| Windows 11 尚未执行本次升级回归 | NFR-002 | H5-011 |

## 6. 已知偏差

- 无。

## 7. 后续任务

- H5-002-C：实现真实网络状态和全局错误映射。
- H5-005-C：在当前依赖基线上接入 Ant Design X/XMarkdown。

## 8. 维护规则

- 新验证完成后更新 §4 和 §5。
- 行为发生变化时直接更新当前状态；历史由 git 保留。
- 不在本文件代签 `status` / `reviewers`。
