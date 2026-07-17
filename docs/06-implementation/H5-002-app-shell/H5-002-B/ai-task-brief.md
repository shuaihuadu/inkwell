---
id: H5-002-B
title: Desktop 公共 AppShell 与权限导航 · AI 任务简报
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
  - H5-002-A
tests:
  - AC-007
downstream:
  - H5-002-C
  - H5-003-A
---

<!-- markdownlint-disable MD025 -->

# H5-002-B · Desktop 公共 AppShell 与权限导航任务简报

> **当前状态**：本任务已经实现并通过 §10 全部验收命令；验证事实见同目录 `implementation-record.md`。本文件保留为实施边界，不应再次重复执行。
>
> **实施范围更新**：Owner 在验收期间明确将关于弹层、主题设置和原型对齐锁屏纳入本任务；以下边界已按最终实现同步，代码基线为 `726ebd6`。

## 1. 任务目标

将认证后的 Header、分组导航、页面容器和登出从 Agent 功能组件抽取为公共 AppShell，并实现工作区、资源中心、仅 Super 用户可见的系统管理导航，以及持久化外观设置和关于弹层。

## 2. 不做范围

- 不改 Agent 列表、创建、聊天或数据查询行为；归 H5-003/H5-005。
- 不实现统一连接状态和全局错误映射；归 H5-002-C。
- 工具、Skills、模型和 Admin 只提供已确认占位入口，不实现业务能力。

## 3. 上游设计引用

- `docs/01-requirements/ui-spec.md` §0.2。
- `docs/01-requirements/requirements.md` §13 第 29、31 条。
- `docs/06-implementation/H5-002-app-shell/scope.md`。
- `prototypes/inkwell-visual-design/src/pages/AppShellExplorer.tsx`：参考最新导航结构和交互语义，不复制 mock 状态。

## 4. 测试引用

暂无独立 H4 TC；临时以 AC-007 和 §9 为验证依据。

## 5. 当前基线与问题

### 5.1 当前实现

- `AgentWorkspace` 同时持有 Header、单层导航、Agent 查询和聊天布局。
- `AppShell` 只负责认证状态分流。

### 5.2 待解决问题

1. 公共外壳与 Agent 业务耦合，后续页面无法共享导航和 Header。
2. 当前只有单层“Agent 库”，未实现已确认的三组导航和权限过滤。

## 6. 允许修改的文件

- `src/app/desktop/src/app-shell.tsx`
- `src/app/desktop/src/features/shell/**`
- `src/app/desktop/src/features/agent-library/agent-workspace.tsx`
- `src/app/desktop/src/features/auth/lock-page.tsx`
- `src/app/desktop/src/index.css`
- `src/app/desktop/src/main.tsx`
- `src/app/desktop/src/shared/network/contracts.ts`
- `src/app/desktop/electron/main.ts`
- `src/app/desktop/electron/preload.ts`
- `src/app/desktop/public/quanzhange.jpg`
- `src/app/desktop/tests/**`

## 7. 禁止修改

- 除应用元数据和锁定时序外，不修改其他 Electron IPC 与网络行为。
- Agent CRUD、ChatPanel、认证状态机和后端代码。
- 不引入 Router；当前切片使用壳层内部页面状态，真实页面路由在 H5-003-C 随设计页/对话页入口统一处理。

## 8. 实现要求

- 新增公共 `WorkspaceShell`，接收 Agent 空间内容，不接管其查询状态。
- Header 高度 56px，显示品牌、后台服务静态基线状态、用户和登出入口。
- 左侧固定 200px，三个分组可独立展开/折叠；不实现已从最新原型移除的整体导航折叠。
- 工作区包含“Agent 空间”；资源中心包含工具、Skills、模型三个“待上线”入口；系统管理只在 `identity.isSuper` 时显示 Admin。
- 非 Agent 入口显示明确占位页，切回 Agent 空间时保留 Agent 组件状态。
- 用户菜单提供亮色、暗色、跟随系统和三套主题色；设置在本机持久化并覆盖工作区与锁屏。
- 关于弹层展示真实应用版本、构建信息和公众号二维码。

## 9. 测试要求

1. 现有登录 E2E 继续进入 Agent 空间。
2. Super 用户看到 Admin，资源入口显示待上线占位，并可返回 Agent 空间。
3. 普通用户看不到系统管理和 Admin。
4. 主题模式与主题色可切换，暗色工作区和锁屏均保持可读。
5. 关于弹层显示二维码；窗口失焦不立即锁定，系统锁屏后可成功解锁。

## 10. 验收命令

```shell
npm --prefix src/app/desktop run build
npm --prefix src/app/desktop run lint
npm --prefix src/app/desktop run test
npm --prefix src/app/desktop run test:e2e
```

## 11. 完成标准

- 公共外壳不再由 AgentWorkspace 持有。
- 权限导航和占位入口符合 §8。
- §10 全部命令通过。

## 12. 风险、假设与待确认项

### 12.1 已知风险

- DOM 重构可能破坏既有登录 E2E；通过语义选择器和新增导航断言控制。

### 12.2 实施假设

- H5-002-C 落地前沿用现有“后台服务正常”静态基线，不新增伪造的重连状态机。

### 12.3 待 Owner 确认

- 无。

## 13. H5 交付格式

完成后返回修改文件、验证摘要、偏差和六字段提交信息草稿，不运行 git 提交命令。
