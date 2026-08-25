---
id: H5-003-B
title: Agent 空间 Owner 删除快捷动作 · AI 任务简报
stage: H5
document_type: task-brief
status: draft
authors:
  - name: GitHub Copilot
    role: agent
reviewers: []
created: 2026-08-25
updated: 2026-08-25
upstream:
  - REQ-002
tests:
  - AC-009
  - AC-010
downstream: []
---

<!-- markdownlint-disable MD025 -->

# H5-003-B · Agent 空间 Owner 删除快捷动作任务简报

> 本文件严格按照 `docs/_templates/implementation-task-brief.template.md` 编写。`status` / `reviewers` 由 Owner 人工维护，Agent 不代签。

## 1. 任务目标

在 UI-003“我的”Agent 卡片 hover 操作中提供仅 Owner 可见的删除入口。删除必须经过明确的危险操作二次确认；确认后永久删除 Agent、全部版本及所有参与用户的会话历史，并立即刷新 Agent 列表。

## 2. 不做范围

- 不修改后端删除语义、持久化级联或公共 API。
- 不改变 Agent 编辑页已有删除入口。
- 不修改分享、撤销分享、克隆、编辑或对话分流。
- 不允许 Admin 删除他人的 Agent。

## 3. 上游设计引用

- `AGENTS.md` §3.2：Renderer 通过 shared network 与 typed preload 调用后端。
- `docs/01-requirements/requirements.md`：REQ-002、§8.3、§8.4、§13 第 33 条。
- `docs/01-requirements/acceptance-criteria.md`：AC-009、AC-010。
- `docs/01-requirements/ui-spec.md`：UI-003 Agent 卡片动作和删除确认。
- `docs/01-requirements/user-flow.md`：UI-003 删除自己 Agent 流程。
- `prototypes/inkwell-visual-design/src/pages/AppShellExplorer.tsx`：UI-003 Owner 卡片删除入口与二次确认原型。
- `prototypes/inkwell-visual-design/src/pages/AgentDesignPage.tsx`：危险删除按钮与二次确认的视觉交互基线。

## 4. 测试引用

暂无独立 H4 TC；临时以 AC-009、AC-010 和 §9 为验证依据。

## 5. 当前基线与问题

### 5.1 当前实现

- WebApi、Electron main/preload 和 `desktopApi.deleteAgent` 已提供 Owner 删除链路。
- Agent 编辑页已有删除确认和成功/失败反馈。
- Agent 空间已有 Owner 编辑、分享、撤销分享动作，但没有删除快捷入口。

### 5.2 待解决问题

1. UI-003 不满足 AC-009 要求的 Owner“编辑 / 删除 / 共享”快捷动作。
2. 用户无法从 Agent 空间按 AC-010 的明确影响范围确认永久删除。

## 6. 允许修改的文件

- `src/app/desktop/src/features/agent-library/agent-workspace.tsx`
- `src/app/desktop/src/shared/i18n/resources.ts`
- `src/app/desktop/tests/login.spec.ts`
- `prototypes/inkwell-visual-design/src/pages/AppShellExplorer.tsx`
- `prototypes/inkwell-visual-design/tests/screenshot.spec.ts`
- `prototypes/inkwell-visual-design/screenshots/32-agent-delete-confirmation.png`
- `prototypes/inkwell-visual-design/screenshots/33-agent-delete-dialog.png`
- `prototypes/inkwell-visual-design/coverage.md`
- `docs/06-implementation/H5-003-agent-space/**`
- `docs/06-implementation/README.md`

## 7. 禁止修改

- `src/core/**`
- `src/app/desktop/electron/**`
- `src/app/desktop/src/shared/network/**`
- EF Core Migration、Entity、Repository 和数据库 schema
- 文档 `status` / `reviewers` 人工签字位

## 8. 实现要求

### 8.1 Owner 删除入口

- 仅“我的”tab 且当前用户为 Owner 时显示删除图标按钮。
- 使用 Ant Design 图标、危险按钮样式和可访问名称；点击不得触发卡片打开。
- 共享 tab 中不得显示删除入口，包括 Admin 用户。

### 8.2 明确二次确认

- 确认弹层必须包含 Agent 名称。
- 正文必须明确说明将永久删除 Agent、全部版本及所有参与用户的会话历史，且不可恢复。
- 取消不得发送 DELETE 请求；确认后调用现有 `desktopApi.deleteAgent`。
- 成功后刷新 Agent 查询并展示成功反馈；失败复用现有全局请求错误语义。

### 8.3 依赖版本策略

- 本任务不新增或升级依赖，复用现有 Ant Design、React Query 和 i18n 基线。

## 9. 测试要求

1. Electron E2E 验证 Owner 卡片显示删除入口，确认弹层包含完整永久删除影响说明。
2. Electron E2E 验证取消后 DELETE 请求数为 0，Agent 仍在列表。
3. Electron E2E 验证确认后只发送一次 DELETE，请求成功后卡片消失并显示成功反馈。
4. Electron E2E 验证他人共享 Agent 不显示删除入口。
5. 原型 Playwright 验证取消、确认及非 Owner 权限，并输出页面级与弹层级设计截图。

## 10. 验收命令

按顺序执行：

```shell
cd prototypes/inkwell-visual-design && npm run build
cd prototypes/inkwell-visual-design && npm exec -- playwright test tests/screenshot.spec.ts --project=desktop-hd --grep "UI-003 confirms Owner Agent deletion"
npm --prefix src/app/desktop run test:e2e -- --grep "shows authentication errors and enters the workspace after login"
npm --prefix src/app/desktop run test
npm --prefix src/app/desktop run lint
npm --prefix src/app/desktop run build
npm --prefix src/app/desktop run test:e2e
dotnet build Inkwell.slnx
git diff --check
```

## 11. 完成标准

- Owner 可从“我的”Agent 卡片发起删除，且必须经过明确二次确认。
- UI-003 原型包含可点击的取消 / 确认流程，并有经过目视检查的设计截图。
- 取消、确认、成功刷新和非 Owner 权限行为符合 §8、§9。
- 中英文固定 UI 文案完整。
- §10 全部命令通过，无新增 warning。
- 实际修改文件是 §6 的子集，§7 保持未修改。

## 12. 风险、假设与待确认项

### 12.1 已知风险

- 删除是跨用户会话历史的不可恢复操作；通过危险样式、完整影响说明和显式确认控制误操作风险。

### 12.2 实施假设

- 后端现有 DELETE 端点及数据库级联已实现 REQ-002 的删除语义，本任务只补 UI-003 入口和验证，不重复实现后端。

### 12.3 待 Owner 确认

- 无；Owner 已明确要求删除时必须确认，并要求遵循既有原型设计。

## 13. H5 交付格式

完成后提供修改文件、验证摘要、偏差和六字段提交信息草稿，不运行 git commit。
