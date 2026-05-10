---
stage: H1
feature: inkwell-agent-platform
status: draft
upstream:
  - docs/01-requirements/ui-spec.md
  - docs/01-requirements/user-flow.md
  - docs/01-requirements/acceptance-criteria.md
  - docs/07-reviews/2026-05-08-openquestion-discussion.md
tech_stack: React 18 + TypeScript 5.6 + Vite 5 + Ant Design 5 (+ ProLayout) + @xyflow/react + react-router-dom (HashRouter) + npm
last_updated: 2026-05-09
---

# Inkwell Agent 平台 · 原型覆盖矩阵

本文件供 `H1-PrototypeReviewer` 直接消费：每条 `UI-NNN` 都能机械核对到原型源码、状态触发深链、截图。

## 1. 项目级决策（用户在 H1-PrototypeAuthor 会话中确认）

| 维度          | 决策                                                           |
| ------------- | -------------------------------------------------------------- |
| 技术栈        | React 18.3 + TypeScript 5.6 + Vite 5.4                         |
| UI Kit        | Ant Design 5.21 + @ant-design/pro-components 2.8（ProLayout）  |
| 路由          | react-router-dom 6.27，HashRouter（兼容未来 Electron file://） |
| 编排画布      | @xyflow/react 12.3（依 OQ-013 closed）                         |
| Mock 数据位置 | `src/mocks/` 单一目录，按域分文件                              |
| 状态切换演示  | URL query `?state=<value>`，可深链分享                         |
| 启动命令      | `npm install && npm run dev`（127.0.0.1:5173）                 |
| 包管理        | npm（用户明确选择，未使用 pnpm）                               |

## 2. UI-NNN × 状态映射

> 约定：状态列与 ui-spec 同名；截图命名 `UI-NNN-<state>.png` 全部位于 `screenshots/`。

### UI-001 登录页

| UI-NNN | ui-spec 节标题 | 原型文件 / 路由                        | 状态                  | 截图                       |
| ------ | -------------- | -------------------------------------- | --------------------- | -------------------------- |
| UI-001 | §1 登录页      | `src/pages/UI001Login.tsx` · `/ui-001` | 默认                  | `UI-001-default.png`       |
| UI-001 | §1 登录页      | 同上                                   | 提交中                | `UI-001-submitting.png`    |
| UI-001 | §1 登录页      | 同上                                   | 账号或密码错误（401） | `UI-001-failed-401.png`    |
| UI-001 | §1 登录页      | 同上                                   | 账号已锁              | `UI-001-failed-locked.png` |
| UI-001 | §1 登录页      | 同上                                   | 速率超限              | `UI-001-failed-rate.png`   |
| UI-001 | §1 登录页      | 同上                                   | 离线 EX-001           | `UI-001-offline.png`       |

### UI-002 锁定页

| UI-NNN | ui-spec 节标题 | 原型文件 / 路由                                                           | 状态     | 截图                 |
| ------ | -------------- | ------------------------------------------------------------------------- | -------- | -------------------- |
| UI-002 | §2 锁定页      | `src/pages/UI002Lock.tsx` · `/ui-002`（演示路由）                         | 演示页   | `UI-002-default.png` |
| UI-002 | §2 锁定页      | `src/layouts/LockOverlay.tsx`（真实全屏遮罩，由顶栏「锁定演示」开关触发） | 锁定遮罩 | `Lock-overlay.png`   |

### UI-003 Agent 库

| UI-NNN | ui-spec 节标题 | 原型文件 / 路由                               | 状态           | 截图                      |
| ------ | -------------- | --------------------------------------------- | -------------- | ------------------------- |
| UI-003 | §3 Agent 库    | `src/pages/UI003AgentLibrary.tsx` · `/ui-003` | 有数据（默认） | `UI-003-data.png`         |
| UI-003 | §3 Agent 库    | 同上                                          | 加载中         | `UI-003-loading.png`      |
| UI-003 | §3 Agent 库    | 同上                                          | 空 · 我的      | `UI-003-empty-mine.png`   |
| UI-003 | §3 Agent 库    | 同上                                          | 空 · 团队共享  | `UI-003-empty-shared.png` |
| UI-003 | §3 Agent 库    | 同上                                          | 空 · 我使用过  | `UI-003-empty-used.png`   |
| UI-003 | §3 Agent 库    | 同上                                          | 出错           | `UI-003-error.png`        |

### UI-004 Agent 配置 / 详情

| UI-NNN | ui-spec 节标题                 | 原型文件 / 路由                                           | 状态                           | 截图                                |
| ------ | ------------------------------ | --------------------------------------------------------- | ------------------------------ | ----------------------------------- |
| UI-004 | §4 Agent 配置                  | `src/pages/UI004AgentConfig.tsx` · `/ui-004?id=agent-001` | 编辑中                         | `UI-004-editing.png`                |
| UI-004 | §4 Agent 配置                  | 同上                                                      | 新建草稿                       | `UI-004-new-draft.png`              |
| UI-004 | §4 Agent 配置                  | 同上                                                      | 只读（非 Owner）               | `UI-004-readonly.png`               |
| UI-004 | §4 Agent 配置                  | 同上                                                      | 提交中                         | `UI-004-submitting.png`             |
| UI-004 | §4 Agent 配置                  | 同上                                                      | 提交失败 EX-001 / EX-002       | `UI-004-submit-failed.png`          |
| UI-004 | §4 Agent 配置                  | 同上                                                      | 提交成功（已保存为 v4）        | `UI-004-submit-success.png`         |
| UI-004 | §4.10 公开 API · OQ-016 closed | 同上                                                      | Token 一次性弹层（勾选才能关） | `UI-004-token-modal.png`            |
| UI-004 | §4.5 Skills · REQ-008 / EX-008 | 同上                                                      | scripts/ 前置拒收弹窗          | `UI-004-skill-scripts-rejected.png` |

### UI-005 对话页

| UI-NNN | ui-spec 节标题                 | 原型文件 / 路由                                            | 状态                                               | 截图                         |
| ------ | ------------------------------ | ---------------------------------------------------------- | -------------------------------------------------- | ---------------------------- |
| UI-005 | §5 对话页                      | `src/pages/UI005Conversation.tsx` · `/ui-005?id=agent-001` | 有数据（含 user / agent / tool / system 四种气泡） | `UI-005-data.png`            |
| UI-005 | §5 对话页                      | 同上                                                       | 加载历史中                                         | `UI-005-loading-history.png` |
| UI-005 | §5 对话页                      | 同上                                                       | 空（新会话）                                       | `UI-005-empty-new.png`       |
| UI-005 | §5 对话页                      | 同上                                                       | 流式回复中（含「停止生成」按钮）                   | `UI-005-streaming.png`       |
| UI-005 | §5 对话页                      | 同上                                                       | 工具调用中                                         | `UI-005-tool-calling.png`    |
| UI-005 | §5.4 多模态 · REQ-016          | 同上                                                       | 录音中（00:12 / 60s 上限）                         | `UI-005-recording.png`       |
| UI-005 | §5.4 多模态 · REQ-016 / AC-062 | 同上                                                       | 转写中                                             | `UI-005-transcribing.png`    |
| UI-005 | §5.4 多模态 · REQ-016          | 同上                                                       | 文件解析中                                         | `UI-005-file-parsing.png`    |
| UI-005 | §5 对话页 · EX-001             | 同上                                                       | 网络异常                                           | `UI-005-error-net.png`       |
| UI-005 | §5 对话页 · EX-002             | 同上                                                       | 模型故障 503                                       | `UI-005-error-model.png`     |
| UI-005 | §5.4 多模态 · EX-004 / AC-061  | 同上                                                       | 图片不兼容（前置拒收）                             | `UI-005-image-incompat.png`  |

### UI-006 编排视图

| UI-NNN | ui-spec 节标题                  | 原型文件 / 路由                                | 状态                | 截图                              |
| ------ | ------------------------------- | ---------------------------------------------- | ------------------- | --------------------------------- |
| UI-006 | §6 编排 · OQ-013 closed         | `src/pages/UI006Orchestration.tsx` · `/ui-006` | 编辑中（DAG 默认）  | `UI-006-editing.png`              |
| UI-006 | §6 编排                         | 同上                                           | 加载中              | `UI-006-loading.png`              |
| UI-006 | §6 编排                         | 同上                                           | 空                  | `UI-006-empty.png`                |
| UI-006 | §6 编排                         | 同上                                           | 校验失败（含循环）  | `UI-006-validation-failed.png`    |
| UI-006 | §6 编排                         | 同上                                           | 运行中              | `UI-006-running.png`              |
| UI-006 | §6 编排 · EX-007                | 同上                                           | 超限终止            | `UI-006-timeout-terminated.png`   |
| UI-006 | §6 编排                         | 同上                                           | 出错                | `UI-006-error.png`                |
| UI-006 | §6 编排 · webhook 一次性 Secret | 同上                                           | webhook Secret 弹层 | `UI-006-webhook-secret-modal.png` |

### UI-007 调试 / Trace 视图

| UI-NNN | ui-spec 节标题                          | 原型文件 / 路由                             | 状态                        | 截图                   |
| ------ | --------------------------------------- | ------------------------------------------- | --------------------------- | ---------------------- |
| UI-007 | §7 trace · OQ-018 closed → 时序树候选 A | `src/pages/UI007TraceDebug.tsx` · `/ui-007` | 有数据（左列表 + 右时序树） | `UI-007-data.png`      |
| UI-007 | §7 trace                                | 同上                                        | 加载中                      | `UI-007-loading.png`   |
| UI-007 | §7 trace                                | 同上                                        | 空                          | `UI-007-empty.png`     |
| UI-007 | §7 trace                                | 同上                                        | 回放中                      | `UI-007-replaying.png` |
| UI-007 | §7 trace                                | 同上                                        | 出错                        | `UI-007-error.png`     |

### UI-008 版本视图

| UI-NNN | ui-spec 节标题         | 原型文件 / 路由                          | 状态                     | 截图                          |
| ------ | ---------------------- | ---------------------------------------- | ------------------------ | ----------------------------- |
| UI-008 | §8 版本 · REQ-015 / S7 | `src/pages/UI008Version.tsx` · `/ui-008` | 默认（左列表 / 右 diff） | `UI-008-default.png`          |
| UI-008 | §8 版本                | 同上                                     | 加载中                   | `UI-008-loading.png`          |
| UI-008 | §8 版本                | 同上                                     | 回滚中                   | `UI-008-rolling-back.png`     |
| UI-008 | §8 版本                | 同上                                     | 回滚成功                 | `UI-008-rollback-success.png` |
| UI-008 | §8 版本                | 同上                                     | 回滚失败                 | `UI-008-rollback-failed.png`  |

### UI-009 Admin 管理页

| UI-NNN | ui-spec 节标题                    | 原型文件 / 路由                        | 状态                               | 截图                       |
| ------ | --------------------------------- | -------------------------------------- | ---------------------------------- | -------------------------- |
| UI-009 | §9 Admin · REQ-017 / NFR-004      | `src/pages/UI009Admin.tsx` · `/ui-009` | 无权限（默认 owner / Member 角色） | `UI-009-no-permission.png` |
| UI-009 | §9.1 账号 tab                     | 同上（顶栏切到 Admin · sa-carol）      | 有数据（账号 tab）                 | `UI-009-accounts.png`      |
| UI-009 | §9.2 共享 tab                     | 同上                                   | 有数据（共享 tab）                 | `UI-009-shares.png`        |
| UI-009 | §9.3 审计日志 tab · OQ-020 closed | 同上                                   | 有数据（审计 tab，无导出按钮）     | `UI-009-audit.png`         |
| UI-009 | §9 Admin                          | 同上                                   | 加载中                             | `UI-009-loading.png`       |
| UI-009 | §9 Admin                          | 同上                                   | 空                                 | `UI-009-empty.png`         |
| UI-009 | §9 Admin                          | 同上                                   | 出错                               | `UI-009-error.png`         |

## 3. ui-spec §4.5 十项自检（交付前）

| #   | 维度         | 是否覆盖 | 备注                                                                    |
| --- | ------------ | -------- | ----------------------------------------------------------------------- |
| 1   | 页面布局     | 已       | 9 个 UI-NNN 都能从顶栏 / 路由打开                                       |
| 2   | 页面状态     | 已       | 已为 9 页 × 各自 4–11 种状态各抓 1 张截图                               |
| 3   | 表单字段     | 已       | UI-001 / UI-004 / UI-006 字段名、必填、长度限制按 ui-spec 落地          |
| 4   | 校验规则     | 已       | UI-001 用户名必填、UI-004 名称 1–50 字符、`max_tokens` ≥ 1 等           |
| 5   | 错误提示文案 | 已       | UI-001 四种错误 / EX-001~008 文案与 ui-spec 一字一致                    |
| 6   | 空状态       | 已       | UI-003 三档 tab 各自空状态 + UI-007 / UI-009 空状态                     |
| 7   | 加载状态     | 已       | 每页 `loading` 状态均可通过 `?state=loading` 触发                       |
| 8   | 权限差异     | 已       | UI-009 无权限 vs Admin；UI-004 编辑 vs 只读 vs 复制为我的；顶栏角色切换 |
| 9   | 关键交互流程 | 已       | 登录→Agent 库→配置→对话→Trace 链路可走通；编排 DAG 可拖拽               |
| 10  | 异常路径     | 已       | EX-001 / EX-002 / EX-004 / EX-006 / EX-007 / EX-008 全部演出            |

## 4. 已知缺口

> 截至 H1-PrototypeAuthor 交付，下列条目仍未在原型中落地或仍属上游问题。`H1-PrototypeReviewer` 评审时请重点关注。

- **空白**：原型只演示 ui-spec 已描述的状态，未实现的少量未列 ui-spec 的"细节态"（例如：UI-005 文件上传成功提示、UI-006 节点详情侧拉抽屉的字段全集）等同于「ui-spec 未列 → 不实现」。
- **OQ-018（trace 视觉形态）**：本原型采用「时序树（候选 A）」作为单一实现，是为让 `PrototypeReviewer` 在评审时复核选型；如评审认为应改为「平铺列表」或「摘要 + 下钻」，需回 `H1-UISpecAuthor` 修源后再回炉。
- **OQ-006 / OQ-007（密码策略 / 锁定阈值）**：仍 `pending`。原型只展示 NFR-003「5 分钟无操作锁定」的结果态，未做密码强度校验 / 失败计数 UI；待两条 OQ 关闭后由 UISpecAuthor 决定是否在登录页与配置页补显式 UI。
- **真实接口与流式数据**：所有响应、流式回复、工具调用、转写均为静态 mock，无真实异步事件序列。
- **多语言 / 无障碍 / 移动端适配**：ui-spec 未描述，原型未补；如未来需要请回 `H1-UISpecAuthor`。
- **视觉风格**：原型用 antd 默认主题；任何品牌色 / 字体 / 间距细化均属设计师工作，不在本 Agent 范围。

## 5. 下一步

请人工切到 `H1-PrototypeReviewer` Agent，对本目录做 PASS / FAIL / UNKNOWN 评审；评审结果由人工写入 `docs/02-prototype/prototype-review.md`。
