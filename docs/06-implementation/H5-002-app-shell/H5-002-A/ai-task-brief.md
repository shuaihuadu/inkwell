---
id: H5-002-A
title: Desktop 前端依赖升级与兼容基线 · AI 任务简报
stage: H5
document_type: task-brief
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

# H5-002-A · Desktop 前端依赖升级与兼容基线任务简报

> **当前状态**：本任务已经实现并通过 §10 全部验收命令；验证事实见同目录 `implementation-record.md`。本文件保留为实施边界，不应再次重复执行。
>
> 本任务只建立后续 AppShell 与聊天页面需要的依赖和兼容基线，不实现新页面。

## 1. 任务目标

将 Desktop 从 Ant Design 5 升级到 Ant Design 6，并安装 Ant Design X 与 XMarkdown，使现有登录、锁屏、Agent 工作区和聊天基线继续通过构建、lint、单元测试与 Electron 登录 E2E。

## 2. 不做范围

- 不实现三组导航、主题设置或全局错误条；归 H5-002-B/C。
- 不接入 Ant Design X 聊天组件或 AG-UI SDK；归 H5-005-C。
- 不改变认证、Agent CRUD、聊天协议或 Electron IPC 公共契约。

## 3. 上游设计引用

- `AGENTS.md` §2.1、§3.2、§3.3：Electron/React 技术栈、安全边界和 v1 禁区。
- `docs/03-architecture/adr/ADR-001-client-runtime-electron-react.md`。
- `docs/06-implementation/H5-002-app-shell/scope.md` §4～8。
- `prototypes/inkwell-visual-design/package.json`：只参考已验证依赖组合，不复制原型运行逻辑。

## 4. 测试引用

暂无独立 H4 TC；临时以 AC-001、AC-076 和 §9 为验证依据。

## 5. 当前基线与问题

### 5.1 当前实现

- Desktop 使用 React 19.2.7、Ant Design 5.29.3、Icons 5.6.1、Vite 6.4.3 和 TypeScript 5.9.3。
- 登录 Electron E2E 已覆盖登录视觉、认证失败和进入工作区。

### 5.2 待解决问题

1. 后续已确认原型依赖 Ant Design 6 与 Ant Design X，产品依赖尚未对齐。
2. 升级必须证明现有页面无 TypeScript、lint 和登录 E2E 回归。

## 6. 允许修改的文件

仅允许修改：

- `src/app/desktop/package.json`
- `src/app/desktop/package-lock.json`
- `src/app/desktop/src/**`
- `src/app/desktop/tests/**`
- `src/app/desktop/vitest.config.*`（仅用于隔离 Electron E2E 与 Vitest）

## 7. 禁止修改

- `src/app/desktop/electron/**` 与 typed preload/IPC 契约。
- `src/core/**`、`tests/**`、`prototypes/**` 和其他 H5 功能域。
- 不升级 Vite、TypeScript、Electron、React、React Query 或 Zustand。
- 不关闭 `contextIsolation`，不启用 `nodeIntegration`。

## 8. 实现要求

### 8.1 依赖升级

- 采用 2026-07-17 npm 查询结果：`antd 6.5.1`、`@ant-design/icons 6.3.2`、`@ant-design/x 2.8.0`、`@ant-design/x-markdown 2.8.0`。
- 使用 npm 更新 manifest 和 lockfile，不手工伪造 lockfile。

### 8.2 兼容修复

- 只修复依赖升级直接引起的类型、API、样式或测试选择器问题。
- 保持现有产品文案、认证状态机、Agent API 和聊天行为不变。
- 若 Vitest 仍会误收集 `tests/login.spec.ts`，增加最小配置将 Electron E2E 排除出 Vitest。

### 8.3 依赖版本策略

- Vite 保持 6.x、TypeScript 保持 5.x，遵守 ADR-001 已锁定主版本。
- 不因 npm 存在更高主版本而扩大本任务。

## 9. 测试要求

1. clean install 后依赖树无 peer dependency 错误。
2. TypeScript/electron-vite 构建通过。
3. ESLint 通过。
4. Vitest 不收集 Playwright E2E，真实单元测试通过。
5. Electron 登录 E2E 验证登录页、错误态和进入工作区。

## 10. 验收命令

从仓库根目录按顺序执行：

```shell
npm --prefix src/app/desktop ci
npm --prefix src/app/desktop run build
npm --prefix src/app/desktop run lint
npm --prefix src/app/desktop run test
npm --prefix src/app/desktop run test:e2e
```

## 11. 完成标准

- 四个目标依赖锁定到 §8.1 版本。
- 现有页面和 Electron 安全边界不变。
- §10 全部命令通过，无新增 warning。
- 实际修改文件是 §6 的子集，§7 保持未修改。

## 12. 风险、假设与待确认项

### 12.1 已知风险

- Ant Design 6 DOM 与样式变化可能使现有 E2E 选择器失效；优先改为语义选择器，不通过降低断言规避回归。

### 12.2 实施假设

- 原型已经验证四个目标版本与 React 19.2.7 兼容。

### 12.3 待 Owner 确认

- 无。

## 13. H5 交付格式

完成后返回修改文件、验证摘要、偏差和六字段提交信息草稿，不运行 git 提交命令。
